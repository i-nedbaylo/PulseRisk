namespace PulseRisk.BackgroundWorkers.LoadTests;

public interface ILoadTestReportWriter
{
    Task WriteAsync(
        LoadTestReport report,
        string path,
        CancellationToken cancellationToken);
}
