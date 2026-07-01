using Microsoft.EntityFrameworkCore;
using PulseRisk.Domain.Entities;

namespace PulseRisk.Infrastructure.Persistence;

public sealed class PulseRiskDbContext(DbContextOptions<PulseRiskDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

    public DbSet<TradingAccount> TradingAccounts => Set<TradingAccount>();

    public DbSet<Instrument> Instruments => Set<Instrument>();

    public DbSet<Trade> Trades => Set<Trade>();

    public DbSet<Position> Positions => Set<Position>();

    public DbSet<Quote> Quotes => Set<Quote>();

    public DbSet<RiskRule> RiskRules => Set<RiskRule>();

    public DbSet<RiskAlert> RiskAlerts => Set<RiskAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PulseRiskDbContext).Assembly);
    }
}

