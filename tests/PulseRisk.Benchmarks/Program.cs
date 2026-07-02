using BenchmarkDotNet.Running;
using PulseRisk.Benchmarks.LatencySampling;

if (args.Contains("--latency-sample", StringComparer.OrdinalIgnoreCase))
{
    var iterations = ReadIntArgument(args, "--iterations", defaultValue: 100_000);
    if (iterations <= 0)
    {
        throw new ArgumentOutOfRangeException(
            nameof(iterations),
            "--iterations must be greater than zero.");
    }

    var output = ReadStringArgument(args, "--output", "docs/benchmarks/2026-07-02-domain-latency-sample.md")
        ?? "docs/benchmarks/2026-07-02-domain-latency-sample.md";
    var sample = DomainCalculationLatencySampler.Run(iterations);
    var outputPath = ResolvePath(output);
    var outputDirectory = Path.GetDirectoryName(outputPath);

    if (!string.IsNullOrWhiteSpace(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }

    await File.WriteAllTextAsync(
        outputPath,
        RenderLatencySample(sample),
        CancellationToken.None);

    Console.WriteLine($"Latency sample written to {outputPath}");
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

static int ReadIntArgument(string[] args, string name, int defaultValue)
{
    var value = ReadStringArgument(args, name, null);

    return int.TryParse(value, out var parsed) ? parsed : defaultValue;
}

static string? ReadStringArgument(string[] args, string name, string? defaultValue)
{
    for (var index = 0; index < args.Length - 1; index++)
    {
        if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[index + 1];
        }
    }

    return defaultValue;
}

static string ResolvePath(string path)
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

static string RenderLatencySample(DomainLatencySample sample)
{
    return $"""
        # P95-выборка задержки: доменные расчеты

        ## Параметры

        - Итерации: `{sample.Iterations}`
        - Таймер: `Stopwatch.GetTimestamp`
        - Единица измерения: наносекунды

        ## Результаты

        | Сценарий | p95 |
        | --- | ---: |
        | `PositionCalculator.ApplyTrade` | {sample.PositionCalculationP95Nanoseconds:F2} ns |
        | `PnLCalculator.CalculateFloatingPnL` | {sample.PnLCalculationP95Nanoseconds:F2} ns |

        ## Примечания

        Этот файл дополняет BenchmarkDotNet-отчет p95-оценкой на фиксированном числе локальных итераций. Для mean и allocations используйте BenchmarkDotNet artifacts.
        """;
}
