# Глава 30. Измеряем производительность и аллокации

## Цель главы

Добавить микро-бенчмарки для доменных расчетов PulseRisk и на их основе сделать маленькую, но обоснованную оптимизацию.

В предыдущих главах мы уже измеряли PostgreSQL-запросы через `EXPLAIN ANALYZE` и весь backend pipeline через load-test worker. Теперь фокус уже:

- не SQL;
- не HTTP;
- не channels;
- не batch writer;
- не background workers.

Фокус главы - чистая доменная математика:

- `PositionCalculator.ApplyTrade`;
- `PnLCalculator.CalculateFloatingPnL`.

Именно такие методы легко недооценить. Они выглядят маленькими, но в real-time risk backend вызываются очень часто: после сделок, после котировок, при пересчете risk metrics и в будущих read models.

## Демонстрируемые компетенции

Глава показывает:

- использование BenchmarkDotNet для mean и allocations;
- разделение macro/load test и micro benchmark;
- создание baseline без ломания production-кода;
- проверку managed allocations в hot path;
- локальную p95-выборку как дополнительный regression signal;
- минимальную оптимизацию после измерений;
- применение GoF/GRASP там, где они помогают, а не ради схемы.

## Что измеряем

Есть два класса benchmark-ов:

```text
PositionCalculationBenchmarks
PnLCalculationBenchmarks
```

Первый сравнивает старую reference-record форму результата с production value-result.

Второй сравнивает object-heavy путь, где для каждого расчета создается `Quote`, с production-путями, где расчет идет по уже имеющейся entity или напрямую по primitive inputs.

## Почему нужен отдельный benchmark-проект

BenchmarkDotNet запускает код иначе, чем unit-тесты:

- прогревает runtime;
- делает несколько итераций;
- считает статистику;
- умеет измерять managed allocations;
- генерирует воспроизводимый отчет.

Поэтому benchmark-и лежат в отдельном проекте:

```text
tests/PulseRisk.Benchmarks
```

Unit-тесты отвечают на вопрос "правильно ли считает формула".

Benchmark-и отвечают на вопрос "сколько стоит этот расчет".

## BenchmarkDotNet setup

Для первой итерации используется короткий job:

```csharp
[MemoryDiagnoser]
[ShortRunJob]
```

`ShortRun` выбран осознанно. Нам нужен быстрый локальный baseline для книги и репозитория. Для release-проверок в CI позже можно добавить более строгий job или отдельный scheduled benchmark pipeline.

`MemoryDiagnoser` важен сильнее, чем сами наносекунды: в high-frequency коде маленькая allocation в 48-88 B может стать регулярным давлением на GC.

## Baseline для позиции

Production-код уже имеет понятный `PositionCalculator`:

```text
PositionCalculationState + trade side + volume + price
-> PositionCalculationResult
```

До оптимизации `PositionCalculationState` и `PositionCalculationResult` были reference records. Это удобно, но каждый результат позиции создавал managed object.

Чтобы сравнение осталось честным после изменения production-кода, benchmark-проект содержит reference-модель:

```text
ReferencePositionCalculationState
ReferencePositionCalculationResult
ReferencePositionCalculator
```

Она не участвует в application flow. Ее единственная задача - сохранить baseline прежнего allocation-heavy подхода.

## Оптимизированный вариант

Production-модели расчета позиции стали:

```csharp
public readonly record struct PositionCalculationState
{
    // ...
}

public readonly record struct PositionCalculationResult(
    decimal NetVolume,
    decimal AveragePrice);
```

Почему `readonly record struct`?

- значение маленькое;
- модель immutable;
- объект не имеет identity;
- модель часто создается в hot path;
- нам не нужны reference semantics.

Почему не обычный `struct`?

`record struct` сохраняет компактную модель данных и удобство value equality. Для этой задачи это хороший баланс между читаемостью и allocation profile.

## PnL baseline

`PnLCalculator` уже был чистым доменным сервисом. Риск был не в формуле, а в способе вызова.

Baseline:

```text
на каждый расчет создать PositionCalculationState
на каждый расчет создать Quote
вызвать CalculateFloatingPnL
```

Production-варианты:

```text
использовать уже загруженную Quote entity
или вызвать primitive overload по bid/ask
```

Такой benchmark хорошо показывает типичную ошибку: оптимизировать формулу, когда настоящая allocation появляется из-за лишнего создания объектов вокруг нее.

## Команда запуска

```bash
dotnet run -c Release --project tests/PulseRisk.Benchmarks -- \
  --filter *CalculationBenchmarks* \
  --artifacts docs/benchmarks/artifacts \
  --join
```

Отчет сохранен в:

```text
docs/benchmarks/2026-07-02-domain-calculations.md
```

Raw artifacts BenchmarkDotNet пишутся в `docs/benchmarks/artifacts`, но не коммитятся: там много временных файлов. В репозиторий кладется короткий markdown-отчет с командами, окружением, таблицей и выводами.

## Результаты

Локальное окружение:

| Параметр | Значение |
| --- | --- |
| BenchmarkDotNet | `0.15.8` |
| OS | Windows 11 `10.0.26200.8655` |
| CPU | Intel Core i5-14400F |
| .NET SDK | `10.0.301` |
| Runtime | `.NET 10.0.9`, X64 RyuJIT |
| Job | `ShortRun` |

BenchmarkDotNet:

| Type | Method | Mean | Allocated |
| --- | --- | ---: | ---: |
| `PnLCalculationBenchmarks` | `ObjectHeavyReferencePath` | 174.08 ns | 88 B |
| `PositionCalculationBenchmarks` | `ReferenceRecordResult` | 45.02 ns | 48 B |
| `PnLCalculationBenchmarks` | `ProductionEntityPath` | 35.70 ns | 0 B |
| `PositionCalculationBenchmarks` | `ProductionValueResult` | 39.44 ns | 0 B |
| `PnLCalculationBenchmarks` | `ProductionPrimitivePath` | 35.55 ns | 0 B |

Главный результат не в том, что один метод быстрее на несколько наносекунд. Главный результат в том, что production-path для расчетов позиции и PnL не создает managed allocations.

## P95-сэмпл

Для p95 добавлен простой режим benchmark-проекта:

```bash
dotnet run -c Release --project tests/PulseRisk.Benchmarks -- \
  --latency-sample \
  --iterations 100000 \
  --output docs/benchmarks/2026-07-02-domain-latency-sample.md
```

Результат:

| Сценарий | p95 |
| --- | ---: |
| `PositionCalculator.ApplyTrade` | 800.00 ns |
| `PnLCalculator.CalculateFloatingPnL` | 100.00 ns |

Эти числа нельзя напрямую сравнивать с BenchmarkDotNet `Mean`. Ручной sampler включает overhead вызова таймера и сортировку выборки делает уже после измерения. Его роль проще: дать локальную p95-точку, которую легко переснять после изменения формул.

## Паттерны GoF/GRASP

### GRASP Information Expert

`PositionCalculator` и `PnLCalculator` остаются владельцами формул.

Проблема, которую это решает: расчет позиции и PnL не должен расползаться по API, EF mappings, repositories и workers.

Почему это не overengineering: calculators маленькие, чистые и покрываются unit-тестами. Они не требуют DI, состояния или наследования.

### GRASP Pure Fabrication

Benchmark-классы и reference-модели не являются частью предметной области.

Они созданы искусственно, чтобы решить инженерную задачу: измерить старый и новый подход рядом, не загрязняя production-код.

Это уместная Pure Fabrication: она повышает проверяемость, а не добавляет runtime-сложность.

### GRASP Protected Variations

Benchmark-проект изолирует экспериментальную reference-модель от production domain.

Если завтра мы поменяем внутреннее представление `PositionCalculationResult`, application flow останется прежним, а benchmark можно будет обновить отдельно.

### GRASP Low Coupling

`PulseRisk.Benchmarks` ссылается только на `PulseRisk.Domain`.

Он не поднимает API, PostgreSQL, DI container или hosted services. Это важно: micro benchmark должен измерять доменную формулу, а не весь application host.

### Почему нет GoF Strategy

В этой главе нет алгоритмической вариативности, похожей на risk rules.

`PositionCalculator` не выбирает одну из многих стратегий. Он выполняет конкретную формулу. Вводить `IPositionCalculationStrategy` ради benchmark-а было бы overengineering.

### Почему нет Factory

`PositionCalculationResult` создается напрямую.

Фабрика была бы полезна, если бы создание результата требовало сложного выбора subtype, внешних зависимостей или повторяющихся инвариантов. Здесь этого нет.

### Почему нет Object Pool

Object Pool мог бы выглядеть соблазнительно рядом со словом allocations.

Но после перехода на `readonly record struct` объект для pool-а просто исчез. Pool усложнил бы код, добавил бы lifecycle-ошибки и не решил бы новую проблему.

## Anti-overengineering вывод

Оптимизация получилась маленькой:

```text
reference record result -> readonly record struct result
```

Именно это делает ее хорошей для проекта.

Мы не переписывали доменную модель на unsafe-код, не вводили object pooling, не добавляли кэш и не строили отдельную performance abstraction. Измерение показало конкретную allocation, и изменение устранило именно ее.

## Что изменилось в репозитории

- Добавлены `PositionCalculationBenchmarks`.
- Добавлены `PnLCalculationBenchmarks`.
- Добавлены reference-модели для baseline.
- Добавлен режим `--latency-sample`.
- `PositionCalculationState` и `PositionCalculationResult` переведены в `readonly record struct`.
- Добавлен `docs/benchmarks/README.md`.
- Добавлен `docs/benchmarks/2026-07-02-domain-calculations.md`.
- Добавлен `docs/benchmarks/2026-07-02-domain-latency-sample.md`.
- Testcontainers preflight теперь быстро пропускает PostgreSQL-сценарии, если локального Docker image нет.
- Обновлены `README.md`, `ARCHITECTURE.md`, `DEVELOPMENT_PLAN.md`, `book/progress.md`.

## Частые ошибки

- Делать выводы о production SLA по `ShortRun` benchmark-у.
- Сравнивать наносекунды с разных машин без контекста окружения.
- Оптимизировать код до появления baseline.
- Считать только mean и не смотреть allocations.
- Создавать benchmark, который случайно измеряет DI, EF Core или logging вместо формулы.
- Добавлять паттерн ради красивой архитектурной схемы.

## Вопросы для интервью

- Почему benchmark-проект зависит только от домена?
- Почему `PositionCalculationResult` лучше как value type?
- Почему `Quote` не нужно создавать внутри каждого PnL-расчета?
- Что показывает `MemoryDiagnoser`?
- Почему p95 sampler не заменяет BenchmarkDotNet?
- Какие оптимизации мы специально не добавили?
- Когда здесь мог бы появиться Strategy?
- Почему object pool был бы лишним?

## Проверка результата

Команда:

```bash
dotnet test PulseRisk.slnx
```

Результат текущей итерации:

- unit-тесты: `73/73`;
- integration tests: `6/9`;
- skipped integration tests: `3/9`.

Три PostgreSQL/Testcontainers-сценария пропущены, потому что в локальном Docker окружении отсутствует image `postgres:17-alpine`.

## Чек-лист главы

- [x] Benchmark для PnL calculation добавлен.
- [x] Benchmark для position calculation добавлен.
- [x] Baseline-вариант добавлен.
- [x] Production/optimized-вариант добавлен.
- [x] Allocations измерены.
- [x] Mean execution time измерено.
- [x] p95 execution time снято отдельным sampler-ом.
- [x] Benchmark report сохранен.
- [x] Выводы описаны.
