# Глава 19. Реализуем bounded channels и backpressure

## Цель главы

Сделать внутренние каналы не только bounded, но и наблюдаемыми: видеть глубину очереди, количество записанных, прочитанных и отброшенных событий, а также корректно завершать каналы при shutdown.

После главы 18 у нас уже были `IEventWriter<T>`, `IEventReader<T>` и `InMemoryEventChannel<T>`. Но без diagnostics перегрузка канала была бы невидимой. В real-time backend это опасно: система может терять котировки или тормозить producers, а мы увидим только косвенные симптомы.

## Что сделано в коде

Application layer:

- `EventChannelSnapshot`;
- `IEventChannelMonitor`;
- `IEventChannelLifetime`.

Infrastructure layer:

- `InMemoryEventChannel<TEvent>` считает:
  - текущую глубину очереди;
  - written messages;
  - read messages;
  - dropped messages;
  - completion state;
- для `DropWrite` заполненный канал явно отбрасывает новое сообщение;
- для `DropOldest`/`DropNewest` заполненная очередь фиксирует dropped counter;
- переполнение канала логируется как `Warning`;
- `EventChannelShutdownService` завершает каналы при остановке host.

Tests:

- `InMemoryEventChannelTests` проверяет `DropWrite` и counters;
- `InMemoryEventChannelTests` проверяет, что completed channel позволяет reader-у дочитать накопленные сообщения.

## Почему нужны counters

Backpressure без метрик трудно обсуждать и отлаживать. Для демонстрационного backend важно показать не только сам `Channel<T>`, но и инженерный контроль:

- насколько заполнена очередь;
- сколько событий producer отправил;
- сколько consumer успел прочитать;
- сколько событий пришлось отбросить;
- завершен ли канал.

Это основа для будущих health checks, Prometheus metrics и load-test reports.

## Поведение full modes

| Full mode | Что делает канал | Где уместно |
| --- | --- | --- |
| `Wait` | Producer ждет свободного места | Risk events, где потеря события хуже задержки |
| `DropWrite` | Новое сообщение отбрасывается, если очередь полна | Optional telemetry/events, где нельзя тормозить producer |
| `DropOldest` | Старое сообщение уступает место новому | Quote stream, где свежая цена важнее устаревшей |
| `DropNewest` | Самый новый buffered item уступает место новой записи | Редкий режим, полезен для специальных очередей |

В `DropWrite` счетчик dropped точный: запись не попадает в канал. В `DropOldest`/`DropNewest` счетчик фиксируется при записи в уже заполненную очередь; это практичная диагностика pressure point для in-process channel.

## Graceful completion

`IEventChannelLifetime.Complete()` завершает writer. После этого consumer может дочитать уже накопленные сообщения, а затем выйти из `ReadAllAsync`.

На остановке приложения `EventChannelShutdownService` проходит по всем зарегистрированным каналам и вызывает `Complete()`. Это не заменяет `CancellationToken`, но дает каналам аккуратный lifecycle hook.

## Паттерны GoF/GRASP

- **Producer-Consumer**: counters описывают состояние границы между producer и consumer.
- **Strategy-like policy**: `EventChannelFullMode` задает поведение при переполнении. Это не GoF Strategy с классами-алгоритмами, а легкая policy enum, потому что классы пока были бы лишними.
- **Indirection**: producers по-прежнему зависят от `IEventWriter<T>`, а diagnostics живут отдельно в `IEventChannelMonitor`.
- **Protected Variations**: будущий Prometheus exporter сможет читать snapshots, не меняя `CreateTradeHandler` и workers.
- **Template Method**: `EventChannelShutdownService` использует lifecycle host-а, не вводя собственный shutdown framework.

Это не overengineering: counters появились только после того, как канал стал реальной архитектурной границей. Мы не добавляем метрики ради витрины; мы делаем перегрузку измеримой.

## Проверка результата

Команды:

```bash
dotnet build PulseRisk.slnx
dotnet test PulseRisk.slnx --no-build
```

Результат текущей итерации:

- unit-тесты: `37/37`;
- integration tests: `5/5`.

## Чек-лист главы

- [x] `EventChannelSnapshot` добавлен.
- [x] `IEventChannelMonitor` добавлен.
- [x] `IEventChannelLifetime` добавлен.
- [x] Канал считает written/read/dropped.
- [x] Канал считает текущую глубину очереди.
- [x] Переполнение канала логируется.
- [x] `DropWrite` покрыт тестом.
- [x] Completion + drain behavior покрыт тестом.
- [x] `EventChannelShutdownService` завершает каналы при shutdown.
