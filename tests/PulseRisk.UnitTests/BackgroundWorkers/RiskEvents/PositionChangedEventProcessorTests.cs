using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PulseRisk.Application.Events;
using PulseRisk.BackgroundWorkers.RiskEvents;

namespace PulseRisk.UnitTests.BackgroundWorkers.RiskEvents;

public sealed class PositionChangedEventProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ShouldPublishRiskEvaluationRequest()
    {
        var writer = new FakeEventWriter<RiskEvaluationRequested>();
        var processor = new PositionChangedEventProcessor(
            writer,
            NullLogger<PositionChangedEventProcessor>.Instance);
        var message = new PositionChangedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EURUSD",
            NetVolume: 2,
            AveragePrice: 1.25m,
            FloatingPnL: 0,
            OccurredAt: new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));

        await processor.ProcessAsync(message, CancellationToken.None);

        writer.Items.Should().ContainSingle();
        var request = writer.Items.Single();
        request.ClientId.Should().Be(message.ClientId);
        request.TradingAccountId.Should().Be(message.TradingAccountId);
        request.Symbol.Should().Be(message.Symbol);
        request.TradeId.Should().Be(message.TradeId);
        request.PositionId.Should().Be(message.PositionId);
        request.Reason.Should().Be("PositionChanged");
        request.RequestedAt.Should().Be(message.OccurredAt);
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
