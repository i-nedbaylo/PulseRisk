using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PulseRisk.Application.Common;
using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;
using PulseRisk.Application.Risk;
using PulseRisk.BackgroundWorkers.RiskEvents;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.Risk;
using PulseRisk.Domain.Risk.Strategies;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.BackgroundWorkers.RiskEvents;

public sealed class RiskEvaluationProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WhenRuleTriggers_ShouldCreateAlertAndPublishEvent()
    {
        var fixture = CreateFixture([
            new RiskRule(Guid.NewGuid(), "Max exposure", RiskRuleType.MaxExposureLimit, 100_000m, RiskSeverity.Critical, isEnabled: true)
        ]);

        await fixture.Processor.ProcessAsync(fixture.Request, CancellationToken.None);

        fixture.RiskAlerts.AddedAlerts.Should().ContainSingle();
        var alert = fixture.RiskAlerts.AddedAlerts.Single();
        alert.AlertType.Should().Be(RiskAlertType.ExposureLimitExceeded);
        alert.Severity.Should().Be(RiskSeverity.Critical);
        alert.CreatedAt.Should().Be(fixture.Request.RequestedAt);

        fixture.UnitOfWork.SaveChangesCalls.Should().Be(1);
        fixture.AlertEvents.Items.Should().ContainSingle();
        fixture.AlertEvents.Items.Single().AlertId.Should().Be(alert.Id);
    }

    [Fact]
    public async Task ProcessAsync_WhenActiveAlertExists_ShouldSuppressDuplicate()
    {
        var fixture = CreateFixture([
            new RiskRule(Guid.NewGuid(), "Max exposure", RiskRuleType.MaxExposureLimit, 100_000m, RiskSeverity.Critical, isEnabled: true)
        ]);
        fixture.RiskAlerts.ActiveKeys.Add((
            fixture.ClientId,
            fixture.TradingAccountId,
            fixture.Symbol.Value,
            RiskAlertType.ExposureLimitExceeded));

        await fixture.Processor.ProcessAsync(fixture.Request, CancellationToken.None);

        fixture.RiskAlerts.AddedAlerts.Should().BeEmpty();
        fixture.UnitOfWork.SaveChangesCalls.Should().Be(0);
        fixture.AlertEvents.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WhenMultipleDifferentRulesTrigger_ShouldCreateMultipleAlertsWithSingleSave()
    {
        var fixture = CreateFixture([
            new RiskRule(Guid.NewGuid(), "Max exposure", RiskRuleType.MaxExposureLimit, 100_000m, RiskSeverity.Critical, isEnabled: true),
            new RiskRule(Guid.NewGuid(), "Margin warning", RiskRuleType.MarginLevelWarning, 900m, RiskSeverity.Warning, isEnabled: true)
        ]);

        await fixture.Processor.ProcessAsync(fixture.Request, CancellationToken.None);

        fixture.RiskAlerts.AddedAlerts.Should().HaveCount(2);
        fixture.RiskAlerts.AddedAlerts.Select(alert => alert.AlertType).Should().BeEquivalentTo([
            RiskAlertType.ExposureLimitExceeded,
            RiskAlertType.MarginLevelWarning
        ]);
        fixture.UnitOfWork.SaveChangesCalls.Should().Be(1);
        fixture.AlertEvents.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcessAsync_WhenTwoRulesCreateSameAlertType_ShouldSuppressDuplicateInCurrentEvaluation()
    {
        var fixture = CreateFixture([
            new RiskRule(Guid.NewGuid(), "Max exposure critical", RiskRuleType.MaxExposureLimit, 100_000m, RiskSeverity.Critical, isEnabled: true),
            new RiskRule(Guid.NewGuid(), "Max exposure warning", RiskRuleType.MaxExposureLimit, 120_000m, RiskSeverity.Warning, isEnabled: true)
        ]);

        await fixture.Processor.ProcessAsync(fixture.Request, CancellationToken.None);

        fixture.RiskAlerts.AddedAlerts.Should().ContainSingle();
        fixture.RiskAlerts.AddedAlerts.Single().AlertType.Should().Be(RiskAlertType.ExposureLimitExceeded);
        fixture.UnitOfWork.SaveChangesCalls.Should().Be(1);
        fixture.AlertEvents.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task ProcessAsync_WhenLatestQuoteDoesNotExist_ShouldSkipWithoutPersistingAlerts()
    {
        var fixture = CreateFixture(
            [
                new RiskRule(Guid.NewGuid(), "Max exposure", RiskRuleType.MaxExposureLimit, 100_000m, RiskSeverity.Critical, isEnabled: true)
            ],
            hasLatestQuote: false);

        await fixture.Processor.ProcessAsync(fixture.Request, CancellationToken.None);

        fixture.RiskAlerts.AddedAlerts.Should().BeEmpty();
        fixture.UnitOfWork.SaveChangesCalls.Should().Be(0);
        fixture.AlertEvents.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WhenNoActiveRulesExist_ShouldNotPersistAlerts()
    {
        var fixture = CreateFixture([]);

        await fixture.Processor.ProcessAsync(fixture.Request, CancellationToken.None);

        fixture.RiskAlerts.AddedAlerts.Should().BeEmpty();
        fixture.UnitOfWork.SaveChangesCalls.Should().Be(0);
        fixture.AlertEvents.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WhenRuleIsDisabled_ShouldNotPersistAlert()
    {
        var fixture = CreateFixture([
            new RiskRule(Guid.NewGuid(), "Disabled max exposure", RiskRuleType.MaxExposureLimit, 100_000m, RiskSeverity.Critical, isEnabled: false)
        ]);

        await fixture.Processor.ProcessAsync(fixture.Request, CancellationToken.None);

        fixture.RiskAlerts.AddedAlerts.Should().BeEmpty();
        fixture.UnitOfWork.SaveChangesCalls.Should().Be(0);
        fixture.AlertEvents.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WhenActiveRuleHasNoStrategy_ShouldSkipWithoutPersistingAlerts()
    {
        var fixture = CreateFixture([
            new RiskRule(Guid.NewGuid(), "Price spike", RiskRuleType.PriceSpikeDetection, 2.5m, RiskSeverity.Warning, isEnabled: true)
        ]);

        await fixture.Processor.ProcessAsync(fixture.Request, CancellationToken.None);

        fixture.RiskAlerts.AddedAlerts.Should().BeEmpty();
        fixture.UnitOfWork.SaveChangesCalls.Should().Be(0);
        fixture.AlertEvents.Items.Should().BeEmpty();
    }

    private static ProcessorFixture CreateFixture(
        IReadOnlyCollection<RiskRule> rules,
        bool hasLatestQuote = true,
        Quote? latestQuote = null)
    {
        var clientId = Guid.NewGuid();
        var tradingAccountId = Guid.NewGuid();
        var symbol = new Symbol("EURUSD");
        var timestamp = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
        latestQuote = hasLatestQuote
            ? latestQuote ?? new Quote(Guid.NewGuid(), symbol, new Price(1.30m), new Price(1.31m), timestamp)
            : null;

        var builder = new RiskEvaluationContextBuilder(
            new FakeTradingAccountRepository(new TradingAccount(
                tradingAccountId,
                clientId,
                new Money(10_000m, CurrencyCode.USD),
                leverage: 100m,
                timestamp)),
            new FakePositionRepository(new Position(
                Guid.NewGuid(),
                clientId,
                tradingAccountId,
                symbol,
                netVolume: 2m,
                averagePrice: 1.25m,
                floatingPnL: 0m,
                timestamp)),
            new FakeInstrumentRepository(new Instrument(symbol, "EUR", "USD", 5, 100_000m, isActive: true)),
            new FakeLatestQuoteReader(latestQuote),
            new FakeRiskRuleRepository(rules));
        var riskAlerts = new FakeRiskAlertRepository();
        var unitOfWork = new FakeUnitOfWork();
        var alertEvents = new FakeEventWriter<RiskAlertRaisedEvent>();
        var processor = new RiskEvaluationProcessor(
            builder,
            new RiskRuleStrategyResolver([
                new MaxExposureRuleStrategy(),
                new MaxLossRuleStrategy(),
                new MarginLevelWarningRuleStrategy()
            ]),
            new RiskAlertFactory(),
            riskAlerts,
            unitOfWork,
            alertEvents,
            NullLogger<RiskEvaluationProcessor>.Instance);
        var request = new RiskEvaluationRequested(
            clientId,
            tradingAccountId,
            symbol.Value,
            TradeId: null,
            PositionId: null,
            Reason: "UnitTest",
            RequestedAt: timestamp);

        return new ProcessorFixture(
            processor,
            request,
            clientId,
            tradingAccountId,
            symbol,
            riskAlerts,
            unitOfWork,
            alertEvents);
    }

    private sealed record ProcessorFixture(
        RiskEvaluationProcessor Processor,
        RiskEvaluationRequested Request,
        Guid ClientId,
        Guid TradingAccountId,
        Symbol Symbol,
        FakeRiskAlertRepository RiskAlerts,
        FakeUnitOfWork UnitOfWork,
        FakeEventWriter<RiskAlertRaisedEvent> AlertEvents);

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
    }

    private sealed class FakeRiskRuleRepository(IReadOnlyCollection<RiskRule> rules) : IRiskRuleRepository
    {
        public Task<IReadOnlyCollection<RiskRule>> ListEnabledAsync(CancellationToken cancellationToken)
        {
            var enabledRules = rules
                .Where(rule => rule.IsEnabled)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<RiskRule>>(enabledRules);
        }
    }

    private sealed class FakeRiskAlertRepository : IRiskAlertRepository
    {
        public HashSet<(Guid ClientId, Guid TradingAccountId, string Symbol, RiskAlertType AlertType)> ActiveKeys { get; } = [];

        public List<RiskAlert> AddedAlerts { get; } = [];

        public Task<bool> ExistsActiveAsync(
            Guid clientId,
            Guid tradingAccountId,
            Symbol symbol,
            RiskAlertType alertType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ActiveKeys.Contains((
                clientId,
                tradingAccountId,
                symbol.Value,
                alertType)));
        }

        public Task AddAsync(RiskAlert alert, CancellationToken cancellationToken)
        {
            AddedAlerts.Add(alert);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;

            return Task.FromResult(1);
        }

        public void ClearChanges()
        {
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
