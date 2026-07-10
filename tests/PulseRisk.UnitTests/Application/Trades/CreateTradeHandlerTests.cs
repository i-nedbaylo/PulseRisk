using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PulseRisk.Application.Common;
using PulseRisk.Application.Events;
using PulseRisk.Application.Positions;
using PulseRisk.Application.Repositories;
using PulseRisk.Application.Trades;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.Application.Trades;

public sealed class CreateTradeHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WhenPositionDoesNotExist_ShouldCreateTradeAndPosition()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var symbol = new Symbol("EURUSD");
        var unitOfWork = new FakeUnitOfWork();
        var trades = new FakeTradeRepository();
        var positions = new FakePositionRepository();
        var events = new FakeEventWriter<PositionChangedEvent>();
        var handler = CreateHandler(
            new FakeClientRepository(new Client(clientId, "Acme Capital", ClientStatus.Active, Now)),
            new FakeTradingAccountRepository(CreateAccount(accountId, clientId)),
            new FakeInstrumentRepository(CreateInstrument(symbol, isActive: true)),
            trades,
            positions,
            unitOfWork,
            events);

        var result = await handler.HandleAsync(
            new CreateTradeCommand(clientId, accountId, symbol.Value, TradeSide.Buy, Volume: 2, OpenPrice: 1.25m),
            CancellationToken.None);

        result.ClientId.Should().Be(clientId);
        result.TradingAccountId.Should().Be(accountId);
        result.Symbol.Should().Be(symbol.Value);
        trades.Items.Should().ContainSingle();
        positions.Items.Should().ContainSingle();
        positions.Items.Single().NetVolume.Should().Be(2);
        positions.Items.Single().AveragePrice.Should().Be(1.25m);
        positions.Items.Single().UpdatedAt.Should().Be(Now);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
        events.Items.Should().ContainSingle();
        events.Items.Single().TradeId.Should().Be(result.Id);
        events.Items.Single().PositionId.Should().Be(positions.Items.Single().Id);
        events.Items.Single().NetVolume.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_WhenPositionExists_ShouldUpdateAveragePrice()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var symbol = new Symbol("EURUSD");
        var existingPosition = new Position(
            Guid.NewGuid(),
            clientId,
            accountId,
            symbol,
            netVolume: 2,
            averagePrice: 1.20m,
            floatingPnL: 0,
            updatedAt: Now);
        var unitOfWork = new FakeUnitOfWork();
        var trades = new FakeTradeRepository();
        var positions = new FakePositionRepository(existingPosition);
        var handler = CreateHandler(
            new FakeClientRepository(new Client(clientId, "Acme Capital", ClientStatus.Active, Now)),
            new FakeTradingAccountRepository(CreateAccount(accountId, clientId)),
            new FakeInstrumentRepository(CreateInstrument(symbol, isActive: true)),
            trades,
            positions,
            unitOfWork);

        await handler.HandleAsync(
            new CreateTradeCommand(clientId, accountId, symbol.Value, TradeSide.Buy, Volume: 1, OpenPrice: 1.50m),
            CancellationToken.None);

        positions.Items.Should().ContainSingle();
        existingPosition.NetVolume.Should().Be(3);
        existingPosition.AveragePrice.Should().Be(1.30m);
        trades.Items.Should().ContainSingle();
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_WhenConcurrencyConflictOccurs_ShouldClearChangesAndRetry()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var symbol = new Symbol("EURUSD");
        var stalePosition = new Position(
            Guid.NewGuid(),
            clientId,
            accountId,
            symbol,
            netVolume: 2,
            averagePrice: 1.20m,
            floatingPnL: 0,
            updatedAt: Now);
        var freshPosition = new Position(
            Guid.NewGuid(),
            clientId,
            accountId,
            symbol,
            netVolume: 4,
            averagePrice: 1.30m,
            floatingPnL: 0,
            updatedAt: Now);
        var unitOfWork = new FakeUnitOfWork
        {
            ConcurrencyFailuresBeforeSuccess = 1
        };
        var trades = new FakeTradeRepository();
        var positions = new FakePositionRepository();
        positions.EnqueueGetResults(stalePosition, freshPosition);
        unitOfWork.OnClearChanges = trades.Clear;
        var events = new FakeEventWriter<PositionChangedEvent>();
        var handler = CreateHandler(
            new FakeClientRepository(new Client(clientId, "Acme Capital", ClientStatus.Active, Now)),
            new FakeTradingAccountRepository(CreateAccount(accountId, clientId)),
            new FakeInstrumentRepository(CreateInstrument(symbol, isActive: true)),
            trades,
            positions,
            unitOfWork,
            events);

        await handler.HandleAsync(
            new CreateTradeCommand(clientId, accountId, symbol.Value, TradeSide.Buy, Volume: 1, OpenPrice: 1.50m),
            CancellationToken.None);

        unitOfWork.SaveChangesCallCount.Should().Be(2);
        unitOfWork.ClearChangesCallCount.Should().Be(1);
        trades.Items.Should().ContainSingle();
        freshPosition.NetVolume.Should().Be(5);
        freshPosition.AveragePrice.Should().Be(1.34m);
        events.Items.Should().ContainSingle();
        events.Items.Single().PositionId.Should().Be(freshPosition.Id);
    }

    [Fact]
    public async Task HandleAsync_WhenAccountBelongsToAnotherClient_ShouldThrowConflict()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var symbol = new Symbol("EURUSD");
        var unitOfWork = new FakeUnitOfWork();
        var trades = new FakeTradeRepository();
        var positions = new FakePositionRepository();
        var handler = CreateHandler(
            new FakeClientRepository(new Client(clientId, "Acme Capital", ClientStatus.Active, Now)),
            new FakeTradingAccountRepository(CreateAccount(accountId, Guid.NewGuid())),
            new FakeInstrumentRepository(CreateInstrument(symbol, isActive: true)),
            trades,
            positions,
            unitOfWork);

        var act = async () => await handler.HandleAsync(
            new CreateTradeCommand(clientId, accountId, symbol.Value, TradeSide.Buy, Volume: 1, OpenPrice: 1.25m),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        trades.Items.Should().BeEmpty();
        positions.Items.Should().BeEmpty();
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WhenInstrumentIsInactive_ShouldThrowConflict()
    {
        var clientId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var symbol = new Symbol("EURUSD");
        var unitOfWork = new FakeUnitOfWork();
        var trades = new FakeTradeRepository();
        var positions = new FakePositionRepository();
        var handler = CreateHandler(
            new FakeClientRepository(new Client(clientId, "Acme Capital", ClientStatus.Active, Now)),
            new FakeTradingAccountRepository(CreateAccount(accountId, clientId)),
            new FakeInstrumentRepository(CreateInstrument(symbol, isActive: false)),
            trades,
            positions,
            unitOfWork);

        var act = async () => await handler.HandleAsync(
            new CreateTradeCommand(clientId, accountId, symbol.Value, TradeSide.Buy, Volume: 1, OpenPrice: 1.25m),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        trades.Items.Should().BeEmpty();
        positions.Items.Should().BeEmpty();
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    private static CreateTradeHandler CreateHandler(
        IClientRepository clients,
        ITradingAccountRepository accounts,
        IInstrumentRepository instruments,
        FakeTradeRepository trades,
        FakePositionRepository positions,
        FakeUnitOfWork unitOfWork,
        FakeEventWriter<PositionChangedEvent>? events = null)
    {
        return new CreateTradeHandler(
            new CreateTradeValidator(),
            clients,
            accounts,
            instruments,
            trades,
            positions,
            unitOfWork,
            new FixedTimeProvider(Now),
            events ?? new FakeEventWriter<PositionChangedEvent>(),
            NullLogger<CreateTradeHandler>.Instance);
    }

    private static TradingAccount CreateAccount(Guid accountId, Guid clientId)
    {
        return new TradingAccount(
            accountId,
            clientId,
            new Money(10000, CurrencyCode.USD),
            leverage: 100,
            createdAt: Now);
    }

    private static Instrument CreateInstrument(Symbol symbol, bool isActive)
    {
        return new Instrument(symbol, "EUR", "USD", digits: 5, contractSize: 100000, isActive);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public int ClearChangesCallCount { get; private set; }

        public int ConcurrencyFailuresBeforeSuccess { get; init; }

        public Action? OnClearChanges { get; set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;

            if (SaveChangesCallCount <= ConcurrencyFailuresBeforeSuccess)
            {
                throw new ConcurrencyConflictException(
                    "Test concurrency conflict.",
                    new InvalidOperationException("Simulated conflict."));
            }

            return Task.FromResult(1);
        }

        public void ClearChanges()
        {
            ClearChangesCallCount++;
            OnClearChanges?.Invoke();
        }
    }

    private sealed class FakeClientRepository(params Client[] clients) : IClientRepository
    {
        private readonly Dictionary<Guid, Client> _clients = clients.ToDictionary(client => client.Id);

        public Task AddAsync(Client client, CancellationToken cancellationToken)
        {
            _clients.Add(client.Id, client);

            return Task.CompletedTask;
        }

        public Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            _clients.TryGetValue(id, out var client);

            return Task.FromResult(client);
        }

        public Task<IReadOnlyCollection<Client>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Client>>(_clients.Values.ToArray());
        }
    }

    private sealed class FakeTradingAccountRepository(params TradingAccount[] accounts) : ITradingAccountRepository
    {
        private readonly Dictionary<Guid, TradingAccount> _accounts = accounts.ToDictionary(account => account.Id);

        public Task AddAsync(TradingAccount account, CancellationToken cancellationToken)
        {
            _accounts.Add(account.Id, account);

            return Task.CompletedTask;
        }

        public Task<TradingAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            _accounts.TryGetValue(id, out var account);

            return Task.FromResult(account);
        }

        public Task<IReadOnlyCollection<TradingAccount>> ListByClientIdAsync(
            Guid clientId,
            CancellationToken cancellationToken)
        {
            var result = _accounts.Values
                .Where(account => account.ClientId == clientId)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<TradingAccount>>(result);
        }
    }

    private sealed class FakeInstrumentRepository(params Instrument[] instruments) : IInstrumentRepository
    {
        private readonly Dictionary<Symbol, Instrument> _instruments =
            instruments.ToDictionary(instrument => instrument.Symbol);

        public Task AddAsync(Instrument instrument, CancellationToken cancellationToken)
        {
            _instruments.Add(instrument.Symbol, instrument);

            return Task.CompletedTask;
        }

        public Task<Instrument?> GetBySymbolAsync(Symbol symbol, CancellationToken cancellationToken)
        {
            _instruments.TryGetValue(symbol, out var instrument);

            return Task.FromResult(instrument);
        }

        public Task<IReadOnlyCollection<Instrument>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Instrument>>(_instruments.Values.ToArray());
        }
    }

    private sealed class FakeTradeRepository : ITradeRepository
    {
        public List<Trade> Items { get; } = [];

        public Task AddAsync(Trade trade, CancellationToken cancellationToken)
        {
            Items.Add(trade);

            return Task.CompletedTask;
        }

        public Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(trade => trade.Id == id));
        }

        public Task<PagedResult<Trade>> SearchAsync(
            GetTradesQuery query,
            CancellationToken cancellationToken)
        {
            var filteredItems = Items.AsEnumerable();

            if (query.ClientId is { } clientId)
            {
                filteredItems = filteredItems.Where(trade => trade.ClientId == clientId);
            }

            if (query.TradingAccountId is { } tradingAccountId)
            {
                filteredItems = filteredItems.Where(trade => trade.TradingAccountId == tradingAccountId);
            }

            if (query.Symbol is { } symbol)
            {
                filteredItems = filteredItems.Where(trade => trade.Symbol == symbol);
            }

            if (query.Side is { } side)
            {
                filteredItems = filteredItems.Where(trade => trade.Side == side);
            }

            if (query.CreatedFrom is { } createdFrom)
            {
                filteredItems = filteredItems.Where(trade => trade.CreatedAt >= createdFrom);
            }

            if (query.CreatedTo is { } createdTo)
            {
                filteredItems = filteredItems.Where(trade => trade.CreatedAt <= createdTo);
            }

            var filteredResult = filteredItems.ToArray();
            var (pageNumber, pageSize) = Pagination.Normalize(query.PageNumber, query.PageSize);
            var pageItems = filteredResult
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Task.FromResult(new PagedResult<Trade>(
                pageItems,
                pageNumber,
                pageSize,
                filteredResult.Length));
        }

        public void Clear()
        {
            Items.Clear();
        }
    }

    private sealed class FakePositionRepository(params Position[] positions) : IPositionRepository
    {
        private readonly Queue<Position?> _getResults = new();

        public List<Position> Items { get; } = [.. positions];

        public void EnqueueGetResults(params Position?[] results)
        {
            foreach (var result in results)
            {
                _getResults.Enqueue(result);
            }
        }

        public Task AddAsync(Position position, CancellationToken cancellationToken)
        {
            Items.Add(position);

            return Task.CompletedTask;
        }

        public Task<Position?> GetByTradingAccountAndSymbolAsync(
            Guid tradingAccountId,
            Symbol symbol,
            CancellationToken cancellationToken)
        {
            if (_getResults.Count > 0)
            {
                return Task.FromResult(_getResults.Dequeue());
            }

            var position = Items.FirstOrDefault(candidate =>
                candidate.TradingAccountId == tradingAccountId && candidate.Symbol == symbol);

            return Task.FromResult(position);
        }

        public Task<IReadOnlyCollection<Position>> ListOpenBySymbolAsync(
            Symbol symbol,
            CancellationToken cancellationToken)
        {
            var result = Items
                .Where(position => position.Symbol == symbol && position.NetVolume != 0)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<Position>>(result);
        }

        public Task<IReadOnlyCollection<Position>> SearchAsync(
            GetPositionsQuery query,
            CancellationToken cancellationToken)
        {
            var result = Items.AsEnumerable();

            if (query.ClientId is { } clientId)
            {
                result = result.Where(position => position.ClientId == clientId);
            }

            if (query.TradingAccountId is { } tradingAccountId)
            {
                result = result.Where(position => position.TradingAccountId == tradingAccountId);
            }

            if (query.Symbol is { } symbol)
            {
                result = result.Where(position => position.Symbol == symbol);
            }

            if (query.OpenOnly)
            {
                result = result.Where(position => position.NetVolume != 0);
            }

            return Task.FromResult<IReadOnlyCollection<Position>>(result.ToArray());
        }
    }

    private sealed class FakeEventWriter<TEvent> : IEventWriter<TEvent>
    {
        public List<TEvent> Items { get; } = [];

        public ValueTask WriteAsync(TEvent message, CancellationToken cancellationToken)
        {
            Items.Add(message);

            return ValueTask.CompletedTask;
        }
    }
}
