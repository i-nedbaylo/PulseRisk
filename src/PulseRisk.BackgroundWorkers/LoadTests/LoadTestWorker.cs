using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PulseRisk.BackgroundWorkers.LoadTests;

internal sealed class LoadTestWorker(
    IServiceScopeFactory scopeFactory,
    ILoadTestReportWriter reportWriter,
    IOptions<LoadTestOptions> options,
    IHostApplicationLifetime applicationLifetime,
    ILogger<LoadTestWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value.ToScenarioSettings();

        if (!settings.Enabled)
        {
            logger.LogInformation("Load test worker is disabled.");
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<LoadTestScenarioRunner>();
            var report = await runner.RunAsync(settings, stoppingToken);

            await reportWriter.WriteAsync(report, settings.ReportPath, stoppingToken);

            logger.LogInformation(
                "Load test report {ScenarioName} written to {ReportPath}.",
                report.ScenarioName,
                settings.ReportPath);
        }
        finally
        {
            if (settings.StopApplicationWhenCompleted)
            {
                applicationLifetime.StopApplication();
            }
        }
    }
}
