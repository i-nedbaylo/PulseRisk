using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PulseRisk.Application.Events;

namespace PulseRisk.BackgroundWorkers.MarketData;

internal sealed class MarketDataSimulatorWorker(
    IOptions<MarketDataOptions> options,
    MarketDataQuoteGenerator generator,
    IEventWriter<QuoteTick> quoteTicks,
    TimeProvider timeProvider,
    ILogger<MarketDataSimulatorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Market data simulator is disabled.");
            return;
        }

        var symbols = settings.GetNormalizedSymbols();
        var interval = CalculateInterval(settings);
        var generatedSinceLastLog = 0L;
        var nextStatisticsAt = timeProvider.GetUtcNow()
            .AddSeconds(settings.StatisticsIntervalSeconds);

        logger.LogInformation(
            "Market data simulator started for {SymbolCount} symbols at {TicksPerSecond} ticks/sec/instrument.",
            symbols.Count,
            settings.TicksPerSecondPerInstrument);

        try
        {
            using var timer = new PeriodicTimer(interval, timeProvider);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var timestamp = timeProvider.GetUtcNow();

                foreach (var symbol in symbols)
                {
                    var tick = generator.NextTick(symbol, settings, timestamp);
                    await quoteTicks.WriteAsync(tick, stoppingToken);
                    generatedSinceLastLog++;
                }

                if (timestamp >= nextStatisticsAt)
                {
                    var quotesPerSecond = generatedSinceLastLog / (double)settings.StatisticsIntervalSeconds;

                    logger.LogInformation(
                        "Market data simulator generated {GeneratedQuotes} quotes in the last {IntervalSeconds} seconds ({QuotesPerSecond:F2} quotes/sec).",
                        generatedSinceLastLog,
                        settings.StatisticsIntervalSeconds,
                        quotesPerSecond);

                    generatedSinceLastLog = 0;
                    nextStatisticsAt = timestamp.AddSeconds(settings.StatisticsIntervalSeconds);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Market data simulator stopping.");
        }

        logger.LogInformation("Market data simulator completed.");
    }

    private static TimeSpan CalculateInterval(MarketDataOptions options)
    {
        var milliseconds = Math.Max(1d, 1000d / options.TicksPerSecondPerInstrument);

        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
