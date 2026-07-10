using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PulseRisk.Application.Events;
using PulseRisk.Application.Positions;
using PulseRisk.Application.Repositories;
using PulseRisk.BackgroundWorkers.MarketData;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.BackgroundWorkers.MarketData;

public sealed class QuoteRiskEvaluationDispatcherTests
{
    [Fact]
    public async Task PublishAsync_WhenOpenPositionsExist_ShouldPublishRiskRequestForLatestTickPerSymbol()
    {
        var clientId = Guid.NewGuid();
        var tradingAccountId = Guid.NewGuid();
        var symbol = new Symbol("EURUSD");
        var position = CreatePosition(clientId, tradingAccountId, symbol, netVolume: 2m);
        var flatPosition = CreatePosition(Guid.NewGuid(), Guid.NewGuid(), symbol, netVolume: 0m);
        var writer = new FakeEventWriter<RiskEvaluationRequested>();
        var dispatcher = new QuoteRiskEvaluationDispatcher(
            new FakePositionRepository(position, flatPosition),
            writer,
            NullLogger<QuoteRiskEvaluationDispatcher>.Instance);
        var olderTick = new QuoteTick(symbol.Value, Bid: 1.10m, Ask: 1.11m, new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero));
        var latestTick = new QuoteTick(symbol.Value, Bid: 1.12m, Ask: 1.13m, olderTick.Timestamp.AddSeconds(1));

        var publishedCount = await dispatcher.PublishAsync([olderTick, latestTick], CancellationToken.None);

        publishedCount.Should().Be(1);
        writer.Items.Should().ContainSingle();
        var request = writer.Items.Single();
        request.ClientId.Should().Be(clientId);
        request.TradingAccountId.Should().Be(tradingAccountId);
        request.Symbol.Should().Be(symbol.Value);
        request.PositionId.Should().Be(position.Id);
        request.TradeId.Should().BeNull();
        request.Reason.Should().Be("QuoteTick");
        request.RequestedAt.Should().Be(latestTick.Timestamp);
    }

    [Fact]
    public async Task PublishAsync_WhenSeveralOpenPositionsExistForSymbol_ShouldPublishRequestPerPosition()
    {
        var symbol = new Symbol("GBPUSD");
        var firstPosition = CreatePosition(Guid.NewGuid(), Guid.NewGuid(), symbol, netVolume: 1m);
        var secondPosition = CreatePosition(Guid.NewGuid(), Guid.NewGuid(), symbol, netVolume: -2m);
        var writer = new FakeEventWriter<RiskEvaluationRequested>();
        var dispatcher = new QuoteRiskEvaluationDispatcher(
            new FakePositionRepository(firstPosition, secondPosition),
            writer,
            NullLogger<QuoteRiskEvaluationDispatcher>.Instance);
        var tick = new QuoteTick(symbol.Value, Bid: 1.25m, Ask: 1.26m, new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero));

        var publishedCount = await dispatcher.PublishAsync([tick], CancellationToken.None);

        publishedCount.Should().Be(2);
        writer.Items.Select(request => request.PositionId).Should().BeEquivalentTo([
            firstPosition.Id,
            secondPosition.Id
        ]);
    }

    [Fact]
    public async Task PublishAsync_WhenNoOpenPositionsExist_ShouldNotPublishRequest()
    {
        var writer = new FakeEventWriter<RiskEvaluationRequested>();
        var dispatcher = new QuoteRiskEvaluationDispatcher(
            new FakePositionRepository(),
            writer,
            NullLogger<QuoteRiskEvaluationDispatcher>.Instance);
        var tick = new QuoteTick("XAUUSD", Bid: 2300m, Ask: 2301m, new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero));

        var publishedCount = await dispatcher.PublishAsync([tick], CancellationToken.None);

        publishedCount.Should().Be(0);
        writer.Items.Should().BeEmpty();
    }

    private static Position CreatePosition(
        Guid clientId,
        Guid tradingAccountId,
        Symbol symbol,
        decimal netVolume)
    {
        return new Position(
            Guid.NewGuid(),
            clientId,
            tradingAccountId,
            symbol,
            netVolume,
            averagePrice: netVolume == 0 ? 0m : 1.20m,
            floatingPnL: 0m,
            updatedAt: new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero));
    }

    private sealed class FakePositionRepository(params Position[] positions) : IPositionRepository
    {
        public Task AddAsync(Position position, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Position?> GetByTradingAccountAndSymbolAsync(
            Guid tradingAccountId,
            Symbol symbol,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<Position>> ListOpenBySymbolAsync(
            Symbol symbol,
            CancellationToken cancellationToken)
        {
            var result = positions
                .Where(position => position.Symbol == symbol && position.NetVolume != 0)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<Position>>(result);
        }

        public Task<IReadOnlyCollection<Position>> SearchAsync(
            GetPositionsQuery query,
            CancellationToken cancellationToken)
        {
            var result = positions.AsEnumerable();

            if (query.ClientId is { } clientId)
            {
                result = result.Where(position => position.ClientId == clientId);
            }

            if (query.TradingAccountId is { } tradingAccountId)
            {
                result = result.Where(position => position.TradingAccountId == tradingAccountId);
            }

            if (query.Symbol is { } symbol)
            {
                result = result.Where(position => position.Symbol == symbol);
            }

            if (query.OpenOnly)
            {
                result = result.Where(position => position.NetVolume != 0);
            }

            return Task.FromResult<IReadOnlyCollection<Position>>(result.ToArray());
        }
    }

    private sealed class FakeEventWriter<TEvent> : IEventWriter<TEvent>
    {
        public List<TEvent> Items { get; } = [];

        public ValueTask WriteAsync(TEvent message, CancellationToken cancellationToken)
        {
            Items.Add(message);

            return ValueTask.CompletedTask;
        }
    }
}
