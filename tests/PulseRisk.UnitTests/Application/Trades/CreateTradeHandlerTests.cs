using FluentAssertions;
using PulseRisk.Application.Common;
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
        var handler = CreateHandler(
            new FakeClientRepository(new Client(clientId, "Acme Capital", ClientStatus.Active, Now)),
            new FakeTradingAccountRepository(CreateAccount(accountId, clientId)),
            new FakeInstrumentRepository(CreateInstrument(symbol, isActive: true)),
            trades,
            positions,
            unitOfWork);

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
        FakeUnitOfWork unitOfWork)
    {
        return new CreateTradeHandler(
            new CreateTradeValidator(),
            clients,
            accounts,
            instruments,
            trades,
            positions,
            unitOfWork,
            new FixedTimeProvider(Now));
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

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;

            return Task.FromResult(1);
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
    }

    private sealed class FakePositionRepository(params Position[] positions) : IPositionRepository
    {
        public List<Position> Items { get; } = [.. positions];

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
            var position = Items.FirstOrDefault(candidate =>
                candidate.TradingAccountId == tradingAccountId && candidate.Symbol == symbol);

            return Task.FromResult(position);
        }
    }
}
