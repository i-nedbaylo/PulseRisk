using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;
using PulseRisk.Infrastructure.Persistence.Converters;

namespace PulseRisk.Infrastructure.Persistence.Configurations;

internal sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("instruments");

        builder.HasKey(instrument => instrument.Symbol);

        builder.Property(instrument => instrument.Symbol)
            .HasConversion(ValueObjectConverters.Symbol)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(instrument => instrument.BaseAsset)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(instrument => instrument.QuoteAsset)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(instrument => instrument.Digits)
            .IsRequired();

        builder.Property(instrument => instrument.ContractSize)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(instrument => instrument.IsActive)
            .IsRequired();

        builder.HasData(
            new Instrument(new Symbol("EURUSD"), "EUR", "USD", 5, 100000m, isActive: true),
            new Instrument(new Symbol("GBPUSD"), "GBP", "USD", 5, 100000m, isActive: true),
            new Instrument(new Symbol("XAUUSD"), "XAU", "USD", 2, 100m, isActive: true));
    }
}

