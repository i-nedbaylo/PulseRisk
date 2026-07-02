# BenchmarkDotNet report: доменные расчеты

Дата локального прогона: `2026-07-02`.

## Цель

Проверить два горячих доменных участка:

- расчет новой позиции при применении сделки;
- расчет floating PnL по позиции и котировке.

Основной вопрос: можно ли уменьшить managed allocations в hot path без усложнения доменной модели.

## Окружение

| Параметр | Значение |
| --- | --- |
| BenchmarkDotNet | `0.15.8` |
| OS | Windows 11 `10.0.26200.8655` |
| CPU | Intel Core i5-14400F, 16 logical / 10 physical cores |
| .NET SDK | `10.0.301` |
| Runtime | `.NET 10.0.9`, X64 RyuJIT x86-64-v3 |
| Job | `ShortRun`, `IterationCount=3`, `LaunchCount=1`, `WarmupCount=3` |

## Команда

```bash
dotnet run -c Release --project tests/PulseRisk.Benchmarks -- \
  --filter *CalculationBenchmarks* \
  --artifacts docs/benchmarks/artifacts \
  --join
```

## Сравниваемые сценарии

| Сценарий | Что измеряет |
| --- | --- |
| `PositionCalculationBenchmarks.ReferenceRecordResult` | Reference baseline: расчет позиции с reference-record result, имитирующий прежний allocation-heavy вариант |
| `PositionCalculationBenchmarks.ProductionValueResult` | Production path после перевода `PositionCalculationState` и `PositionCalculationResult` в `readonly record struct` |
| `PnLCalculationBenchmarks.ObjectHeavyReferencePath` | Object-heavy baseline: создание state/quote перед каждым расчетом |
| `PnLCalculationBenchmarks.ProductionEntityPath` | Production path с уже загруженной `Quote` entity |
| `PnLCalculationBenchmarks.ProductionPrimitivePath` | Primitive overload без передачи `Quote` entity |

## Результаты BenchmarkDotNet

| Type | Method | Mean | StdDev | Gen0 | Allocated |
| --- | --- | ---: | ---: | ---: | ---: |
| `PnLCalculationBenchmarks` | `ObjectHeavyReferencePath` | 174.08 ns | 3.355 ns | 0.0083 | 88 B |
| `PositionCalculationBenchmarks` | `ReferenceRecordResult` | 45.02 ns | 0.894 ns | 0.0046 | 48 B |
| `PnLCalculationBenchmarks` | `ProductionEntityPath` | 35.70 ns | 0.702 ns | 0 | 0 B |
| `PositionCalculationBenchmarks` | `ProductionValueResult` | 39.44 ns | 1.018 ns | 0 | 0 B |
| `PnLCalculationBenchmarks` | `ProductionPrimitivePath` | 35.55 ns | 0.037 ns | 0 | 0 B |

## P95-сэмпл

Дополнительный локальный p95-сэмпл сохранен в `2026-07-02-domain-latency-sample.md`.

| Сценарий | p95 |
| --- | ---: |
| `PositionCalculator.ApplyTrade` | 800.00 ns |
| `PnLCalculator.CalculateFloatingPnL` | 100.00 ns |

Эта выборка намеренно проще BenchmarkDotNet и включает overhead ручного измерения через `Stopwatch.GetTimestamp`. Ее нельзя сравнивать напрямую с `Mean` из BenchmarkDotNet. Она полезна как локальный regression signal.

## Выводы

`PositionCalculationState` и `PositionCalculationResult` оставлены как `readonly record struct`. Это убирает allocation `48 B` на расчет результата позиции и не ухудшает читаемость доменной модели.

`PnLCalculator` уже имел primitive overload. Benchmark подтверждает, что hot path должен переиспользовать загруженную `Quote` или передавать bid/ask напрямую, а не создавать новые entities внутри расчета.

Дополнительные оптимизации не добавлялись:

- SIMD не нужен для единичных decimal-расчетов;
- object pool не нужен для immutable value-result;
- cache не нужен для чистой формулы;
- unsafe/span-подходы не дают ценности для текущей доменной логики.

Следующий performance-шаг находится выше уровнем: Docker Compose, повторяемое окружение и CI/CD artifacts для тестов, benchmark-отчетов и load-test результатов.
