# Benchmark reports

В этой папке лежат воспроизводимые отчеты по микро-бенчмаркам доменных расчетов.

Цель benchmark-документов - показать не абстрактные "быстрые числа", а инженерный цикл:

- определить горячий участок;
- зафиксировать baseline;
- измерить mean и allocations через BenchmarkDotNet;
- снять локальную p95-выборку;
- сделать минимальную оптимизацию только там, где она подтверждается измерением;
- описать выводы рядом с командой запуска.

## Отчеты

- `2026-07-02-domain-calculations.md` - основной отчет BenchmarkDotNet по `PositionCalculator` и `PnLCalculator`.
- `2026-07-02-domain-latency-sample.md` - локальная p95-выборка для доменных расчетов.

## Запуск BenchmarkDotNet

```bash
dotnet run -c Release --project tests/PulseRisk.Benchmarks -- \
  --filter *CalculationBenchmarks* \
  --artifacts docs/benchmarks/artifacts \
  --join
```

Папка `docs/benchmarks/artifacts` игнорируется git-ом через общее правило `artifacts/`, потому что BenchmarkDotNet пишет туда временные файлы и raw reports. В репозиторий сохраняются только отобранные markdown-отчеты.

## Запуск p95-сэмпла

```bash
dotnet run -c Release --project tests/PulseRisk.Benchmarks -- \
  --latency-sample \
  --iterations 100000 \
  --output docs/benchmarks/2026-07-02-domain-latency-sample.md
```

p95-сэмпл не заменяет BenchmarkDotNet. Он нужен как простой дополнительный regression signal: если после изменения формул p95 внезапно вырастает в разы, это повод вернуться к BenchmarkDotNet и профилировщику.
