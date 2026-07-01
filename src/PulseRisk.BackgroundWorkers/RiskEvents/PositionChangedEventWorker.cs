using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PulseRisk.Application.Events;

namespace PulseRisk.BackgroundWorkers.RiskEvents;

internal sealed class PositionChangedEventWorker(
    IEventReader<PositionChangedEvent> positionChangedEvents,
    PositionChangedEventProcessor processor,
    ILogger<PositionChangedEventWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Position changed event worker started.");

        try
        {
            await foreach (var message in positionChangedEvents.ReadAllAsync(stoppingToken))
            {
                await processor.ProcessAsync(message, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Position changed event worker stopped.");
        }

        logger.LogInformation("Position changed event worker completed.");
    }
}
