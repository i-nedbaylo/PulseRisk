using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PulseRisk.Application.Events;
using PulseRisk.Infrastructure;

namespace PulseRisk.UnitTests.Infrastructure.Events;

public sealed class InMemoryEventChannelTests
{
    [Fact]
    public async Task WriteAsync_WhenDropWriteChannelIsFull_ShouldCountDroppedMessage()
    {
        using var provider = CreateProvider(
            ("EventChannels:QuoteCapacity", "1"),
            ("EventChannels:QuoteFullMode", "DropWrite"));
        var writer = provider.GetRequiredService<IEventWriter<QuoteTick>>();
        var reader = provider.GetRequiredService<IEventReader<QuoteTick>>();
        var monitor = GetMonitor<QuoteTick>(provider);
        var lifetime = GetLifetime<QuoteTick>(provider);

        await writer.WriteAsync(CreateTick("EURUSD"), CancellationToken.None);
        await writer.WriteAsync(CreateTick("GBPUSD"), CancellationToken.None);
        lifetime.Complete();

        var messages = await ReadAllAsync(reader);
        var snapshot = monitor.GetSnapshot();

        messages.Should().ContainSingle();
        messages.Single().Symbol.Should().Be("EURUSD");
        snapshot.Capacity.Should().Be(1);
        snapshot.FullMode.Should().Be(EventChannelFullMode.DropWrite);
        snapshot.WrittenMessages.Should().Be(1);
        snapshot.ReadMessages.Should().Be(1);
        snapshot.DroppedMessages.Should().Be(1);
        snapshot.CurrentDepth.Should().Be(0);
        snapshot.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_WhenWaitChannelIsCompleted_ShouldLetReaderDrainMessages()
    {
        using var provider = CreateProvider(
            ("EventChannels:RiskCapacity", "2"),
            ("EventChannels:RiskFullMode", "Wait"));
        var writer = provider.GetRequiredService<IEventWriter<RiskEvaluationRequested>>();
        var reader = provider.GetRequiredService<IEventReader<RiskEvaluationRequested>>();
        var lifetime = GetLifetime<RiskEvaluationRequested>(provider);

        await writer.WriteAsync(CreateRiskRequest("EURUSD"), CancellationToken.None);
        await writer.WriteAsync(CreateRiskRequest("GBPUSD"), CancellationToken.None);
        lifetime.Complete();

        var messages = await ReadAllAsync(reader);

        messages.Select(message => message.Symbol)
            .Should()
            .Equal("EURUSD", "GBPUSD");
    }

    private static ServiceProvider CreateProvider(params (string Key, string Value)[] settings)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PulseRisk"] = "Host=localhost;Port=5432;Database=pulserisk;Username=pulserisk;Password=pulserisk"
        };

        foreach (var (key, value) in settings)
        {
            values[key] = value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddPulseRiskInfrastructure(configuration)
            .BuildServiceProvider();
    }

    private static IEventChannelMonitor GetMonitor<TEvent>(IServiceProvider provider)
    {
        return provider.GetServices<IEventChannelMonitor>()
            .Single(monitor => monitor.EventName == typeof(TEvent).Name);
    }

    private static IEventChannelLifetime GetLifetime<TEvent>(IServiceProvider provider)
    {
        return provider.GetServices<IEventChannelLifetime>()
            .Single(lifetime => lifetime.EventName == typeof(TEvent).Name);
    }

    private static async Task<IReadOnlyCollection<TEvent>> ReadAllAsync<TEvent>(
        IEventReader<TEvent> reader)
    {
        var messages = new List<TEvent>();

        await foreach (var message in reader.ReadAllAsync(CancellationToken.None))
        {
            messages.Add(message);
        }

        return messages;
    }

    private static QuoteTick CreateTick(string symbol)
    {
        return new QuoteTick(
            symbol,
            Bid: 1.10m,
            Ask: 1.11m,
            Timestamp: new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));
    }

    private static RiskEvaluationRequested CreateRiskRequest(string symbol)
    {
        return new RiskEvaluationRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            symbol,
            TradeId: null,
            PositionId: null,
            Reason: "Test",
            RequestedAt: new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));
    }
}
