using PulseRisk.Application.Common;
using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Risk;
using PulseRisk.Domain.Services;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Application.Risk;

public sealed class RiskEvaluationContextBuilder(
    ITradingAccountRepository accounts,
    IPositionRepository positions,
    IInstrumentRepository instruments,
    ILatestQuoteReader latestQuotes,
    IRiskRuleRepository riskRules)
{
    public async Task<RiskEvaluationPreparation> BuildAsync(
        RiskEvaluationRequested request,
        CancellationToken cancellationToken)
    {
        var symbol = new Symbol(request.Symbol);
        var account = await accounts.GetByIdAsync(request.TradingAccountId, cancellationToken)
            ?? throw new EntityNotFoundException("TradingAccount", request.TradingAccountId.ToString());

        if (account.ClientId != request.ClientId)
        {
            throw new ConflictException(
                $"Trading account '{account.Id}' does not belong to client '{request.ClientId}'.");
        }

        var position = await positions.GetByTradingAccountAndSymbolAsync(
            request.TradingAccountId,
            symbol,
            cancellationToken);

        if (position is null)
        {
            return RiskEvaluationPreparation.Skipped(
                request,
                $"Position '{request.TradingAccountId}/{symbol.Value}' was not found.");
        }

        var instrument = await instruments.GetBySymbolAsync(symbol, cancellationToken)
            ?? throw new EntityNotFoundException("Instrument", symbol.Value);

        var latestQuote = await latestQuotes.GetLatestAsync(symbol, cancellationToken);
        if (latestQuote is null)
        {
            return RiskEvaluationPreparation.Skipped(
                request,
                $"Latest quote for '{symbol.Value}' was not found.");
        }

        var activeRules = await riskRules.ListEnabledAsync(cancellationToken);
        var floatingPnL = PnLCalculator.CalculateFloatingPnL(
            position.NetVolume,
            position.AveragePrice,
            latestQuote.Bid,
            latestQuote.Ask,
            instrument.ContractSize);

        var netExposure = Math.Abs(position.NetVolume) * position.AveragePrice * instrument.ContractSize;
        var marginUsed = netExposure / account.Leverage;
        var equity = account.Balance + floatingPnL;
        var marginLevel = marginUsed == 0
            ? decimal.MaxValue
            : equity / marginUsed * 100m;

        var metrics = new RiskMetricSnapshot(
            request.ClientId,
            request.TradingAccountId,
            symbol,
            netExposure,
            floatingPnL,
            equity,
            marginUsed,
            marginLevel);

        var context = new RiskEvaluationContext(
            request.ClientId,
            request.TradingAccountId,
            symbol,
            metrics.NetExposure,
            metrics.FloatingPnL,
            metrics.MarginLevel);

        return RiskEvaluationPreparation.Ready(request, metrics, context, activeRules);
    }
}
