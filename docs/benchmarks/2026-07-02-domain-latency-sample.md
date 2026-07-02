# P95-выборка задержки: доменные расчеты

## Параметры

- Итерации: `100000`
- Таймер: `Stopwatch.GetTimestamp`
- Единица измерения: наносекунды

## Результаты

| Сценарий | p95 |
| --- | ---: |
| `PositionCalculator.ApplyTrade` | 800.00 ns |
| `PnLCalculator.CalculateFloatingPnL` | 100.00 ns |

## Примечания

Этот файл дополняет BenchmarkDotNet-отчет p95-оценкой на фиксированном числе локальных итераций. Для mean и allocations используйте BenchmarkDotNet artifacts.