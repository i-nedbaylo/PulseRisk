using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PulseRisk.Application.Events;

namespace PulseRisk.Infrastructure.Events;

internal sealed class EventChannelShutdownService(
    IEnumerable<IEventChannelLifetime> channels,
    ILogger<EventChannelShutdownService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var channel in channels)
        {
            logger.LogInformation("Completing event channel {EventName}.", channel.EventName);
            channel.Complete();
        }

        return Task.CompletedTask;
    }
}
