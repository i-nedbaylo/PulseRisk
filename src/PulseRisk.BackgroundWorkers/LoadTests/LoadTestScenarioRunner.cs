using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PulseRisk.Application.Accounts;
using PulseRisk.Application.Clients;
using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;
using PulseRisk.Application.Trades;
using PulseRisk.BackgroundWorkers.MarketData;
using PulseRisk.Domain.Enums;

namespace PulseRisk.BackgroundWorkers.LoadTests;

public sealed class LoadTestScenarioRunner(
    IServiceScopeFactory scopeFactory,
    IEventWriter<QuoteTick> quoteTicks,
    IEnumerable<IEventChannelMonitor> channelMonitors,
    IOptions<QuoteBatchOptions> quoteBatchOptions,
    TimeProvider timeProvider,
    ILogger<LoadTestScenarioRunner> logger)
{
    private static readonly TimeSpan RateSlice = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan DrainPollInterval = TimeSpan.FromMilliseconds(250);

    public async Task<LoadTestReport> RunAsync(
        LoadTestScenarioSettings settings,
        CancellationToken cancellationToken)
    {
        var scenarioName = $"local-{settings.Profile}-{timeProvider.GetUtcNow():yyyyMMddHHmmss}";
        var startedAt = timeProvider.GetUtcNow();
        var startSnapshots = CaptureSnapshots();
        var tradeLatencies = new List<double>();
        var tradeLatencyGate = new object();
        var statistics = new LoadTestStatistics();

        logger.LogInformation(
            "Load test {ScenarioName} starting. Profile {Profile}, clients {ClientCount}, accounts/client {AccountsPerClient}, quotes/sec {QuotesPerSecond}, trades/sec {TradesPerSecond}, duration {DurationSeconds}s.",
            scenarioName,
            settings.Profile,
            settings.ClientCount,
            settings.AccountsPerClient,
            settings.QuotesPerSecond,
            settings.TradesPerSecond,
            settings.Duration.TotalSeconds);

        var accounts = await SeedAccountsAsync(settings, cancellationToken);
        var generationStopwatch = Stopwatch.StartNew();

        await Task.WhenAll(
            GenerateQuotesAsync(settings, statistics, cancellationToken),
            GenerateTradesAsync(settings, accounts, statistics, tradeLatencies, tradeLatencyGate, cancellationToken));

        generationStopwatch.Stop();
        var drainDuration = await WaitForDrainAsync(settings.DrainTime, cancellationToken);
        var finishedAt = timeProvider.GetUtcNow();
        var endSnapshots = CaptureSnapshots();
        var deltas = CalculateDeltas(startSnapshots, endSnapshots);
        var activeAlerts = await CountActiveAlertsAsync(cancellationToken);
        var generationDuration = generationStopwatch.Elapsed;
        var scenarioDurationSeconds = Math.Max(
            0.001d,
            generationDuration.Add(drainDuration).TotalSeconds);
        var riskEvaluationDelta = deltas
            .SingleOrDefault(delta => delta.EventName == nameof(RiskEvaluationRequested));
        var quoteDelta = deltas
            .SingleOrDefault(delta => delta.EventName == nameof(QuoteTick));
        var latencySnapshot = SnapshotLatencies(tradeLatencies, tradeLatencyGate);

        var report = new LoadTestReport(
            scenarioName,
            settings.Profile,
            startedAt,
            finishedAt,
            generationDuration,
            drainDuration,
            settings.ClientCount,
            accounts.Count,
            settings.QuotesPerSecond,
            settings.TradesPerSecond,
            Volatile.Read(ref statistics.QuotesGenerated),
            Volatile.Read(ref statistics.TradesAttempted),
            Volatile.Read(ref statistics.TradesSucceeded),
            Volatile.Read(ref statistics.TradesFailed),
            Volatile.Read(ref statistics.QuotesGenerated) / Math.Max(0.001d, generationDuration.TotalSeconds),
            Volatile.Read(ref statistics.TradesSucceeded) / Math.Max(0.001d, generationDuration.TotalSeconds),
            (riskEvaluationDelta?.ReadMessages ?? 0) / scenarioDurationSeconds,
            CalculateAverage(latencySnapshot),
            CalculateP95(latencySnapshot),
            quoteDelta?.DroppedMessages ?? 0,
            activeAlerts,
            deltas);

        logger.LogInformation(
            "Load test {ScenarioName} completed. Quotes {QuotesGenerated}, trades {TradesSucceeded}/{TradesAttempted}, dropped quotes {DroppedQuotes}, active alerts {ActiveAlerts}.",
            report.ScenarioName,
            report.QuotesGenerated,
            report.TradesSucceeded,
            report.TradesAttempted,
            report.DroppedQuotes,
            report.ActiveAlerts);

        return report;
    }

    private async Task<IReadOnlyCollection<LoadTestAccount>> SeedAccountsAsync(
        LoadTestScenarioSettings settings,
        CancellationToken cancellationToken)
    {
        var accounts = new List<LoadTestAccount>(settings.ClientCount * settings.AccountsPerClient);

        for (var clientIndex = 0; clientIndex < settings.ClientCount; clientIndex++)
        {
            var client = await CreateClientAsync(
                $"Load Client {timeProvider.GetUtcNow():yyyyMMddHHmmss}-{clientIndex + 1}",
                cancellationToken);

            for (var accountIndex = 0; accountIndex < settings.AccountsPerClient; accountIndex++)
            {
                var account = await CreateAccountAsync(
                    client.Id,
                    settings.InitialBalance,
                    settings.Leverage,
                    cancellationToken);

                accounts.Add(new LoadTestAccount(client.Id, account.Id));
            }
        }

        return accounts;
    }

    private async Task<ClientDto> CreateClientAsync(
        string name,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateClientHandler>();

        return await handler.HandleAsync(new CreateClientCommand(name), cancellationToken);
    }

    private async Task<TradingAccountDto> CreateAccountAsync(
        Guid clientId,
        decimal initialBalance,
        decimal leverage,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateTradingAccountHandler>();

        return await handler.HandleAsync(
            new CreateTradingAccountCommand(clientId, initialBalance, CurrencyCode.USD, leverage),
            cancellationToken);
    }

    private async Task GenerateQuotesAsync(
        LoadTestScenarioSettings settings,
        LoadTestStatistics statistics,
        CancellationToken cancellationToken)
    {
        var symbols = settings.Symbols.ToArray();

        await GenerateAtRateAsync(
            settings.QuotesPerSecond,
            settings.Duration,
            async (sequence, token) =>
            {
                var symbol = symbols[(int)(sequence % symbols.Length)];
                await quoteTicks.WriteAsync(CreateQuoteTick(symbol, sequence), token);
                Interlocked.Increment(ref statistics.QuotesGenerated);
            },
            cancellationToken);
    }

    private async Task GenerateTradesAsync(
        LoadTestScenarioSettings settings,
        IReadOnlyCollection<LoadTestAccount> accounts,
        LoadTestStatistics statistics,
        List<double> tradeLatencies,
        object tradeLatencyGate,
        CancellationToken cancellationToken)
    {
        var accountArray = accounts.ToArray();
        var symbols = settings.Symbols.ToArray();

        await GenerateAtRateAsync(
            settings.TradesPerSecond,
            settings.Duration,
            async (sequence, token) =>
            {
                Interlocked.Increment(ref statistics.TradesAttempted);
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var account = accountArray[(int)(sequence % accountArray.Length)];
                    var symbol = symbols[(int)(sequence % symbols.Length)];

                    await CreateTradeAsync(
                        account,
                        symbol,
                        sequence,
                        token);

                    Interlocked.Increment(ref statistics.TradesSucceeded);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Interlocked.Increment(ref statistics.TradesFailed);
                    logger.LogWarning(exception, "Load test trade generation failed at sequence {Sequence}.", sequence);
                }
                finally
                {
                    stopwatch.Stop();

                    lock (tradeLatencyGate)
                    {
                        tradeLatencies.Add(stopwatch.Elapsed.TotalMilliseconds);
                    }
                }
            },
            cancellationToken);
    }

    private async Task CreateTradeAsync(
        LoadTestAccount account,
        string symbol,
        long sequence,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateTradeHandler>();
        var side = sequence % 2 == 0 ? TradeSide.Buy : TradeSide.Sell;
        var volume = ((sequence % 5) + 1) / 10m;
        var price = CreatePrice(symbol, sequence);

        await handler.HandleAsync(
            new CreateTradeCommand(
                account.ClientId,
                account.TradingAccountId,
                symbol,
                side,
                volume,
                price),
            cancellationToken);
    }

    private async Task GenerateAtRateAsync(
        int itemsPerSecond,
        TimeSpan duration,
        Func<long, CancellationToken, ValueTask> produceAsync,
        CancellationToken cancellationToken)
    {
        if (itemsPerSecond <= 0)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var produced = 0L;

        while (stopwatch.Elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var expected = (long)Math.Floor(stopwatch.Elapsed.TotalSeconds * itemsPerSecond);
            while (produced < expected)
            {
                await produceAsync(produced, cancellationToken);
                produced++;
            }

            var remaining = duration - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(remaining < RateSlice ? remaining : RateSlice, cancellationToken);
        }
    }

    private async Task<TimeSpan> WaitForDrainAsync(
        TimeSpan drainTime,
        CancellationToken cancellationToken)
    {
        if (drainTime <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var stopwatch = Stopwatch.StartNew();
        var minimumDrainTime = TimeSpan.FromMilliseconds(
            Math.Min(drainTime.TotalMilliseconds, quoteBatchOptions.Value.FlushIntervalMilliseconds + 250d));

        while (stopwatch.Elapsed < drainTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshots = CaptureSnapshots();
            var pendingWork = snapshots.Values.Any(snapshot =>
                snapshot.EventName is nameof(QuoteTick) or nameof(PositionChangedEvent) or nameof(RiskEvaluationRequested)
                && snapshot.CurrentDepth > 0);

            if (!pendingWork && stopwatch.Elapsed >= minimumDrainTime)
            {
                break;
            }

            var remaining = drainTime - stopwatch.Elapsed;
            await Task.Delay(remaining < DrainPollInterval ? remaining : DrainPollInterval, cancellationToken);
        }

        stopwatch.Stop();

        return stopwatch.Elapsed;
    }

    private async Task<long> CountActiveAlertsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var riskAlerts = scope.ServiceProvider.GetRequiredService<IRiskAlertRepository>();

        return await riskAlerts.CountActiveAsync(cancellationToken);
    }

    private Dictionary<string, EventChannelSnapshot> CaptureSnapshots()
    {
        return channelMonitors
            .Select(monitor => monitor.GetSnapshot())
            .ToDictionary(snapshot => snapshot.EventName, StringComparer.Ordinal);
    }

    private static IReadOnlyCollection<EventChannelDelta> CalculateDeltas(
        IReadOnlyDictionary<string, EventChannelSnapshot> startSnapshots,
        IReadOnlyDictionary<string, EventChannelSnapshot> endSnapshots)
    {
        return endSnapshots.Values
            .Select(end =>
            {
                startSnapshots.TryGetValue(end.EventName, out var start);

                return new EventChannelDelta(
                    end.EventName,
                    end.Capacity,
                    end.FullMode,
                    start?.CurrentDepth ?? 0,
                    end.CurrentDepth,
                    end.WrittenMessages - (start?.WrittenMessages ?? 0),
                    end.ReadMessages - (start?.ReadMessages ?? 0),
                    end.DroppedMessages - (start?.DroppedMessages ?? 0));
            })
            .ToArray();
    }

    private QuoteTick CreateQuoteTick(string symbol, long sequence)
    {
        var bid = CreatePrice(symbol, sequence);
        var ask = bid + GetSpread(symbol);

        return new QuoteTick(symbol, bid, ask, timeProvider.GetUtcNow());
    }

    private static decimal CreatePrice(string symbol, long sequence)
    {
        var offset = (sequence % 1000) / 100_000m;

        return symbol switch
        {
            "XAUUSD" => 2300m + ((sequence % 1000) / 10m),
            "GBPUSD" => 1.30m + offset,
            _ => 1.25m + offset
        };
    }

    private static decimal GetSpread(string symbol)
    {
        return symbol == "XAUUSD" ? 0.10m : 0.0001m;
    }

    private static double[] SnapshotLatencies(List<double> latencies, object gate)
    {
        lock (gate)
        {
            return latencies.ToArray();
        }
    }

    private static double CalculateAverage(IReadOnlyCollection<double> values)
    {
        return values.Count == 0 ? 0d : values.Average();
    }

    private static double CalculateP95(IReadOnlyCollection<double> values)
    {
        if (values.Count == 0)
        {
            return 0d;
        }

        var sorted = values.OrderBy(value => value).ToArray();
        var index = Math.Clamp(
            (int)Math.Ceiling(sorted.Length * 0.95d) - 1,
            0,
            sorted.Length - 1);

        return sorted[index];
    }

    private sealed class LoadTestStatistics
    {
        public long QuotesGenerated;

        public long TradesAttempted;

        public long TradesSucceeded;

        public long TradesFailed;
    }
}
