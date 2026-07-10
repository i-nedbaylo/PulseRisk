using FluentAssertions;
using PulseRisk.Application.Events;
using PulseRisk.Application.Positions;
using PulseRisk.Application.Repositories;
using PulseRisk.Application.Risk;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.Application.Risk;

public sealed class RiskEvaluationContextBuilderTests
{
    [Fact]
    public async Task BuildAsync_WhenPositionAndQuoteExist_ShouldBuildRiskMetricSnapshot()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var symbol = new Symbol("EURUSD");
        var timestamp = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
        var builder = CreateBuilder(
            account: new TradingAccount(
                accountId,
                clientId,
                new Money(10_000m, CurrencyCode.USD),
                leverage: 100m,
                timestamp),
            position: new Position(
                Guid.NewGuid(),
                clientId,
                accountId,
                symbol,
                netVolume: 2m,
                averagePrice: 1.25m,
                floatingPnL: 0m,
                timestamp),
            instrument: new Instrument(symbol, "EUR", "USD", 5, 100_000m, isActive: true),
            latestQuote: new Quote(Guid.NewGuid(), symbol, new Price(1.30m), new Price(1.31m), timestamp),
            rules: [
                new RiskRule(Guid.NewGuid(), "Max exposure", RiskRuleType.MaxExposureLimit, 100_000m, RiskSeverity.Critical, isEnabled: true)
            ]);
        var request = new RiskEvaluationRequested(
            clientId,
            accountId,
            symbol.Value,
            TradeId: null,
            PositionId: null,
            Reason: "UnitTest",
            RequestedAt: timestamp);

        var result = await builder.BuildAsync(request, CancellationToken.None);

        result.CanEvaluate.Should().BeTrue();
        result.ActiveRules.Should().ContainSingle();
        result.Metrics.Should().NotBeNull();
        result.Metrics!.NetExposure.Should().Be(250_000m);
        result.Metrics.FloatingPnL.Should().Be(10_000m);
        result.Metrics.Equity.Should().Be(20_000m);
        result.Metrics.MarginUsed.Should().Be(2_500m);
        result.Metrics.MarginLevel.Should().Be(800m);
        result.Context.Should().NotBeNull();
        result.Context!.NetExposure.Should().Be(result.Metrics.NetExposure);
        result.Context.FloatingPnL.Should().Be(result.Metrics.FloatingPnL);
        result.Context.MarginLevel.Should().Be(result.Metrics.MarginLevel);
    }

    [Fact]
    public async Task BuildAsync_WhenLatestQuoteDoesNotExist_ShouldSkipEvaluation()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var symbol = new Symbol("EURUSD");
        var timestamp = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
        var builder = CreateBuilder(
            account: new TradingAccount(
                accountId,
                clientId,
                new Money(10_000m, CurrencyCode.USD),
                leverage: 100m,
                timestamp),
            position: new Position(
                Guid.NewGuid(),
                clientId,
                accountId,
                symbol,
                netVolume: 2m,
                averagePrice: 1.25m,
                floatingPnL: 0m,
                timestamp),
            instrument: new Instrument(symbol, "EUR", "USD", 5, 100_000m, isActive: true),
            latestQuote: null,
            rules: []);
        var request = new RiskEvaluationRequested(
            clientId,
            accountId,
            symbol.Value,
            TradeId: null,
            PositionId: null,
            Reason: "UnitTest",
            RequestedAt: timestamp);

        var result = await builder.BuildAsync(request, CancellationToken.None);

        result.CanEvaluate.Should().BeFalse();
        result.SkipReason.Should().Contain("Latest quote");
        result.Metrics.Should().BeNull();
        result.Context.Should().BeNull();
    }

    private static RiskEvaluationContextBuilder CreateBuilder(
        TradingAccount account,
        Position position,
        Instrument instrument,
        Quote? latestQuote,
        IReadOnlyCollection<RiskRule> rules)
    {
        return new RiskEvaluationContextBuilder(
            new FakeTradingAccountRepository(account),
            new FakePositionRepository(position),
            new FakeInstrumentRepository(instrument),
            new FakeLatestQuoteReader(latestQuote),
            new FakeRiskRuleRepository(rules));
    }

    private sealed class FakeTradingAccountRepository(TradingAccount account) : ITradingAccountRepository
    {
        public Task AddAsync(TradingAccount accountToAdd, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TradingAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(id == account.Id ? account : null);
        }

        public Task<IReadOnlyCollection<TradingAccount>> ListByClientIdAsync(
            Guid clientId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakePositionRepository(Position position) : IPositionRepository
    {
        public Task AddAsync(Position positionToAdd, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Position?> GetByTradingAccountAndSymbolAsync(
            Guid tradingAccountId,
            Symbol symbol,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                position.TradingAccountId == tradingAccountId && position.Symbol == symbol
                    ? position
                    : null);
        }

        public Task<IReadOnlyCollection<Position>> ListOpenBySymbolAsync(
            Symbol symbol,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Position> result =
                position.Symbol == symbol && position.NetVolume != 0
                    ? [position]
                    : [];

            return Task.FromResult(result);
        }

        public Task<IReadOnlyCollection<Position>> SearchAsync(
            GetPositionsQuery query,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Position> result =
                query.ClientId is null || position.ClientId == query.ClientId.Value
                ? [position]
                : [];

            return Task.FromResult<IReadOnlyCollection<Position>>(result);
        }
    }

    private sealed class FakeInstrumentRepository(Instrument instrument) : IInstrumentRepository
    {
        public Task AddAsync(Instrument instrumentToAdd, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Instrument?> GetBySymbolAsync(Symbol symbol, CancellationToken cancellationToken)
        {
            return Task.FromResult(instrument.Symbol == symbol ? instrument : null);
        }

        public Task<IReadOnlyCollection<Instrument>> ListAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeLatestQuoteReader(Quote? quote) : ILatestQuoteReader
    {
        public Task<Quote?> GetLatestAsync(Symbol symbol, CancellationToken cancellationToken)
        {
            return Task.FromResult(quote?.Symbol == symbol ? quote : null);
        }

        public Task<IReadOnlyCollection<Quote>> ListLatestAsync(
            Symbol? symbol,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Quote> result =
                quote is not null && (symbol is null || quote.Symbol == symbol.Value)
                    ? [quote]
                    : [];

            return Task.FromResult(result);
        }
    }

    private sealed class FakeRiskRuleRepository(IReadOnlyCollection<RiskRule> rules) : IRiskRuleRepository
    {
        public Task<IReadOnlyCollection<RiskRule>> ListEnabledAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(rules);
        }
    }
}
