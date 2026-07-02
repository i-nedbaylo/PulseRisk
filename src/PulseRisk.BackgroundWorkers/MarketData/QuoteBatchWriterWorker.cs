using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;

namespace PulseRisk.BackgroundWorkers.MarketData;

internal sealed class QuoteBatchWriterWorker(
    IEventReader<QuoteTick> quoteTicks,
    IServiceScopeFactory scopeFactory,
    IOptions<QuoteBatchOptions> options,
    TimeProvider timeProvider,
    ILogger<QuoteBatchWriterWorker> logger) : BackgroundService
{
    private readonly object _batchGate = new();
    private readonly SemaphoreSlim _flushGate = new(1, 1);
    private readonly List<QuoteTick> _batch = new(Math.Max(1, options.Value.BatchSize));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        logger.LogInformation(
            "Quote batch writer started. BatchSize {BatchSize}, FlushIntervalMs {FlushIntervalMilliseconds}.",
            settings.BatchSize,
            settings.FlushIntervalMilliseconds);

        var channelCompleted = false;

        try
        {
            using var linkedStopping = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            var readTask = ReadQuotesAsync(settings, linkedStopping.Token);
            var flushTask = FlushPeriodicallyAsync(settings, linkedStopping.Token);
            var completedTask = await Task.WhenAny(readTask, flushTask);

            if (completedTask == readTask && readTask.IsCompletedSuccessfully)
            {
                channelCompleted = true;
                linkedStopping.Cancel();
            }
            else if (completedTask.IsFaulted)
            {
                linkedStopping.Cancel();
            }

            await Task.WhenAll(readTask, flushTask);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested || channelCompleted)
        {
            logger.LogInformation("Quote batch writer stopping.");
        }
        finally
        {
            await FlushCurrentBatchAsync(force: true, settings, CancellationToken.None);
        }

        logger.LogInformation("Quote batch writer completed.");
    }

    private async Task ReadQuotesAsync(
        QuoteBatchOptions settings,
        CancellationToken stoppingToken)
    {
        await foreach (var tick in quoteTicks.ReadAllAsync(stoppingToken))
        {
            lock (_batchGate)
            {
                _batch.Add(tick);
            }

            await FlushCurrentBatchAsync(force: false, settings, stoppingToken);
        }
    }

    private async Task FlushPeriodicallyAsync(
        QuoteBatchOptions settings,
        CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(settings.FlushIntervalMilliseconds);
        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await FlushCurrentBatchAsync(force: true, settings, stoppingToken);
        }
    }

    private async Task FlushCurrentBatchAsync(
        bool force,
        QuoteBatchOptions settings,
        CancellationToken cancellationToken)
    {
        QuoteTick[] batchToWrite;

        lock (_batchGate)
        {
            if (_batch.Count == 0 || (!force && _batch.Count < settings.BatchSize))
            {
                return;
            }

            batchToWrite = _batch.ToArray();
            _batch.Clear();
        }

        await WriteBatchAsync(batchToWrite, cancellationToken);
    }

    private async Task WriteBatchAsync(
        IReadOnlyCollection<QuoteTick> batch,
        CancellationToken cancellationToken)
    {
        await _flushGate.WaitAsync(cancellationToken);

        try
        {
            var startedAt = Stopwatch.GetTimestamp();
            await using var scope = scopeFactory.CreateAsyncScope();
            var writer = scope.ServiceProvider.GetRequiredService<IQuoteBatchWriter>();
            var riskDispatcher = scope.ServiceProvider.GetRequiredService<QuoteRiskEvaluationDispatcher>();

            await writer.WriteAsync(batch, cancellationToken);
            var riskRequestCount = await riskDispatcher.PublishAsync(batch, cancellationToken);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            logger.LogInformation(
                "Quote batch writer flushed {QuoteCount} quotes and published {RiskRequestCount} risk requests in {ElapsedMilliseconds} ms.",
                batch.Count,
                riskRequestCount,
                elapsed.TotalMilliseconds);
        }
        finally
        {
            _flushGate.Release();
        }
    }
}
