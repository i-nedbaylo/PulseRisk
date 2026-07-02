using FluentAssertions;
using PulseRisk.Application.Events;
using PulseRisk.BackgroundWorkers.LoadTests;

namespace PulseRisk.UnitTests.BackgroundWorkers.LoadTests;

public sealed class MarkdownLoadTestReportWriterTests
{
    [Fact]
    public async Task WriteAsync_ShouldCreateMarkdownReport()
    {
        var reportPath = Path.Combine(Path.GetTempPath(), $"pulserisk-load-report-{Guid.NewGuid():N}.md");
        var writer = new MarkdownLoadTestReportWriter();
        var report = new LoadTestReport(
            "local-test",
            LoadTestProfile.Quotes500,
            new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 2, 12, 0, 5, TimeSpan.Zero),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(1),
            ClientCount: 2,
            AccountCount: 2,
            QuotesPerSecondTarget: 500,
            TradesPerSecondTarget: 25,
            QuotesGenerated: 1500,
            TradesAttempted: 75,
            TradesSucceeded: 75,
            TradesFailed: 0,
            ActualQuotesPerSecond: 500,
            ActualTradesPerSecond: 25,
            RiskEvaluationsPerSecond: 20,
            AverageTradeLatencyMilliseconds: 5.25,
            P95TradeLatencyMilliseconds: 9.75,
            DroppedQuotes: 0,
            ActiveAlerts: 1,
            [
                new EventChannelDelta(
                    nameof(QuoteTick),
                    Capacity: 4096,
                    EventChannelFullMode.DropOldest,
                    StartDepth: 0,
                    EndDepth: 0,
                    WrittenMessages: 1500,
                    ReadMessages: 1500,
                    DroppedMessages: 0)
            ]);

        try
        {
            await writer.WriteAsync(report, reportPath, CancellationToken.None);

            var markdown = await File.ReadAllTextAsync(reportPath);
            markdown.Should().Contain("# Отчет нагрузочного теста: local-test");
            markdown.Should().Contain("| Сгенерировано котировок | 1500 |");
            markdown.Should().Contain("`QuoteTick`");
        }
        finally
        {
            if (File.Exists(reportPath))
            {
                File.Delete(reportPath);
            }
        }
    }
}
