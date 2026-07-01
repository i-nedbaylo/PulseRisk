using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseRisk.Domain.Entities;
using PulseRisk.Infrastructure.Persistence.Converters;

namespace PulseRisk.Infrastructure.Persistence.Configurations;

internal sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("trades");

        builder.HasKey(trade => trade.Id);

        builder.Property(trade => trade.Id)
            .ValueGeneratedNever();

        builder.Property(trade => trade.Symbol)
            .HasConversion(ValueObjectConverters.Symbol)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(trade => trade.Side)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(trade => trade.Volume)
            .HasConversion(ValueObjectConverters.Volume)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(trade => trade.OpenPrice)
            .HasConversion(ValueObjectConverters.Price)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(trade => trade.CreatedAt)
            .IsRequired();

        builder.HasIndex(trade => new { trade.ClientId, trade.CreatedAt })
            .IsDescending(false, true);

        builder.HasIndex(trade => new { trade.Symbol, trade.CreatedAt })
            .IsDescending(false, true);

        builder.HasIndex(trade => new { trade.TradingAccountId, trade.CreatedAt })
            .IsDescending(false, true);

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(trade => trade.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TradingAccount>()
            .WithMany()
            .HasForeignKey(trade => trade.TradingAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Instrument>()
            .WithMany()
            .HasForeignKey(trade => trade.Symbol)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
