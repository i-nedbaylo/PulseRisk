using System.Globalization;
using System.Text;

namespace PulseRisk.BackgroundWorkers.LoadTests;

public sealed class MarkdownLoadTestReportWriter : ILoadTestReportWriter
{
    public async Task WriteAsync(
        LoadTestReport report,
        string path,
        CancellationToken cancellationToken)
    {
        var resolvedPath = ResolvePath(path);
        var directory = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(resolvedPath, Render(report), cancellationToken);
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var directory = current; directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PulseRisk.slnx")))
            {
                return Path.Combine(directory.FullName, path);
            }
        }

        return Path.Combine(current.FullName, path);
    }

    private static string Render(LoadTestReport report)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Отчет нагрузочного теста: {report.ScenarioName}");
        builder.AppendLine();
        builder.AppendLine("## Сценарий");
        builder.AppendLine();
        builder.AppendLine($"- Profile: `{report.Profile}`");
        builder.AppendLine($"- Начало: `{report.StartedAt:O}`");
        builder.AppendLine($"- Завершение: `{report.FinishedAt:O}`");
        builder.AppendLine($"- Клиенты: `{report.ClientCount}`");
        builder.AppendLine($"- Счета: `{report.AccountCount}`");
        builder.AppendLine($"- Целевые котировки/sec: `{report.QuotesPerSecondTarget}`");
        builder.AppendLine($"- Целевые сделки/sec: `{report.TradesPerSecondTarget}`");
        builder.AppendLine($"- Длительность генерации: `{FormatMilliseconds(report.GenerationDuration)}`");
        builder.AppendLine($"- Drain window: `{FormatMilliseconds(report.DrainDuration)}`");
        builder.AppendLine();
        builder.AppendLine("## Пропускная Способность");
        builder.AppendLine();
        builder.AppendLine("| Метрика | Значение |");
        builder.AppendLine("| --- | ---: |");
        builder.AppendLine($"| Сгенерировано котировок | {report.QuotesGenerated.ToString(CultureInfo.InvariantCulture)} |");
        builder.AppendLine($"| Попыток создания сделок | {report.TradesAttempted.ToString(CultureInfo.InvariantCulture)} |");
        builder.AppendLine($"| Успешных сделок | {report.TradesSucceeded.ToString(CultureInfo.InvariantCulture)} |");
        builder.AppendLine($"| Ошибок сделок | {report.TradesFailed.ToString(CultureInfo.InvariantCulture)} |");
        builder.AppendLine($"| Фактические котировки/sec | {FormatDouble(report.ActualQuotesPerSecond)} |");
        builder.AppendLine($"| Фактические сделки/sec | {FormatDouble(report.ActualTradesPerSecond)} |");
        builder.AppendLine($"| Risk evaluations/sec | {FormatDouble(report.RiskEvaluationsPerSecond)} |");
        builder.AppendLine($"| Dropped quotes | {report.DroppedQuotes.ToString(CultureInfo.InvariantCulture)} |");
        builder.AppendLine($"| Active alerts | {report.ActiveAlerts.ToString(CultureInfo.InvariantCulture)} |");
        builder.AppendLine();
        builder.AppendLine("## Задержка Сделок");
        builder.AppendLine();
        builder.AppendLine("| Метрика | Миллисекунды |");
        builder.AppendLine("| --- | ---: |");
        builder.AppendLine($"| Average | {FormatDouble(report.AverageTradeLatencyMilliseconds)} |");
        builder.AppendLine($"| p95 | {FormatDouble(report.P95TradeLatencyMilliseconds)} |");
        builder.AppendLine();
        builder.AppendLine("## Дельты Каналов");
        builder.AppendLine();
        builder.AppendLine("| Channel | Capacity | Full mode | Start depth | End depth | Written | Read | Dropped |");
        builder.AppendLine("| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |");

        foreach (var delta in report.ChannelDeltas.OrderBy(delta => delta.EventName))
        {
            builder.AppendLine(
                $"| `{delta.EventName}` | {delta.Capacity} | `{delta.FullMode}` | {delta.StartDepth} | {delta.EndDepth} | {delta.WrittenMessages} | {delta.ReadMessages} | {delta.DroppedMessages} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Примечания");
        builder.AppendLine();
        builder.AppendLine("Этот отчет является локальным инженерным артефактом, а не production SLA. Используйте его для сравнения изменений на одной машине, одной базе, одной конфигурации и одном наборе данных.");

        return builder.ToString();
    }

    private static string FormatMilliseconds(TimeSpan value)
    {
        return $"{value.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture)} ms";
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }
}
