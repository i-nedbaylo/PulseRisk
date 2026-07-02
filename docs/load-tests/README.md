# Нагрузочные тесты

Этот каталог хранит локальные нагрузочные отчеты PulseRisk.

Нагрузочный сценарий реализован как отключаемый `LoadTestWorker`. По умолчанию он не запускается, чтобы обычный старт API не создавал данные и не нагружал PostgreSQL.

## Что делает сценарий

При включении `LoadTest:Enabled=true` worker:

- создает клиентов через `CreateClientHandler`;
- создает торговые счета через `CreateTradingAccountHandler`;
- генерирует сделки через `CreateTradeHandler`;
- генерирует `QuoteTick` в quote channel;
- дает `QuoteBatchWriterWorker` и Risk Engine время обработать накопившиеся события;
- собирает channel deltas из `IEventChannelMonitor`;
- считает throughput, trade latency, dropped quotes и active alerts;
- сохраняет markdown-отчет.

## Профили

| Profile | Target quotes/sec | Default trades/sec | Назначение |
| --- | ---: | ---: | --- |
| `Quotes500` | 500 | 25 | локальный smoke/performance run |
| `Quotes1000` | 1000 | 50 | проверка устойчивости pipeline |
| `Quotes5000` | 5000 | 100 | optional senior scenario для сильной машины и подготовленной БД |

`QuotesPerSecond` и `TradesPerSecond` можно переопределить явно.

## Пример запуска

Сначала примените миграции к disposable PostgreSQL:

```bash
dotnet ef database update --project src/PulseRisk.Infrastructure --startup-project src/PulseRisk.Api --connection "$PULSERISK_CONNECTION"
```

Затем запустите API с включенным load test:

```bash
ConnectionStrings__PulseRisk="$PULSERISK_CONNECTION" \
LoadTest__Enabled=true \
LoadTest__StopApplicationWhenCompleted=true \
LoadTest__Profile=Quotes500 \
LoadTest__ClientCount=5 \
LoadTest__AccountsPerClient=1 \
LoadTest__DurationSeconds=3 \
LoadTest__DrainSeconds=5 \
LoadTest__QuotesPerSecond=500 \
LoadTest__TradesPerSecond=10 \
LoadTest__ReportPath=docs/load-tests/YYYY-MM-DD-local-load-report.md \
dotnet run --project src/PulseRisk.Api --no-build --no-launch-profile
```

На Windows PowerShell задайте те же значения через `$env:...`.

## Локальный результат 2026-07-02

Файл:

- `2026-07-02-local-load-report.md`

Параметры:

- disposable PostgreSQL 18 cluster;
- `Quotes500`;
- 5 клиентов;
- 5 счетов;
- 3 секунды генерации;
- 500 target quotes/sec;
- 10 target trades/sec;
- 5 секунд drain window.

Ключевые результаты:

| Метрика | Значение |
| --- | ---: |
| Quotes generated | 1463 |
| Trades succeeded | 28 |
| Actual quotes/sec | 486.94 |
| Actual trades/sec | 9.32 |
| Risk evaluations/sec | 12.54 |
| Dropped quotes | 0 |
| Active alerts | 0 |
| Average trade latency | 11.04 ms |
| p95 trade latency | 16.55 ms |

Это локальный инженерный артефакт, а не production SLA. Его смысл - дать воспроизводимую baseline-точку для будущих оптимизаций.
