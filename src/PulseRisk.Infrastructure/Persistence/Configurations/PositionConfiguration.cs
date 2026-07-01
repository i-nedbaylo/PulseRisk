using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseRisk.Domain.Entities;
using PulseRisk.Infrastructure.Persistence.Converters;

namespace PulseRisk.Infrastructure.Persistence.Configurations;

internal sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions");

        builder.HasKey(position => position.Id);

        builder.Property(position => position.Id)
            .ValueGeneratedNever();

        builder.Property<uint>("xmin")
            .IsRowVersion();

        builder.Property(position => position.Symbol)
            .HasConversion(ValueObjectConverters.Symbol)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(position => position.NetVolume)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(position => position.AveragePrice)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(position => position.FloatingPnL)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(position => position.UpdatedAt)
            .IsRequired();

        builder.HasIndex(position => new { position.ClientId, position.Symbol });

        builder.HasIndex(position => new { position.TradingAccountId, position.Symbol })
            .IsUnique();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(position => position.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TradingAccount>()
            .WithMany()
            .HasForeignKey(position => position.TradingAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Instrument>()
            .WithMany()
            .HasForeignKey(position => position.Symbol)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
