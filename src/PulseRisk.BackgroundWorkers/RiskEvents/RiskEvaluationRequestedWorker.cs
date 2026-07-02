using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PulseRisk.Application.Events;

namespace PulseRisk.BackgroundWorkers.RiskEvents;

internal sealed class RiskEvaluationRequestedWorker(
    IEventReader<RiskEvaluationRequested> riskEvaluationRequests,
    IServiceScopeFactory scopeFactory,
    ILogger<RiskEvaluationRequestedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Risk evaluation worker started.");

        try
        {
            await foreach (var request in riskEvaluationRequests.ReadAllAsync(stoppingToken))
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<RiskEvaluationProcessor>();
                await processor.ProcessAsync(request, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Risk evaluation worker stopped.");
        }

        logger.LogInformation("Risk evaluation worker completed.");
    }
}
