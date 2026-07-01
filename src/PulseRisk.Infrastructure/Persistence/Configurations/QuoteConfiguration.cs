using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseRisk.Domain.Entities;
using PulseRisk.Infrastructure.Persistence.Converters;

namespace PulseRisk.Infrastructure.Persistence.Configurations;

internal sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("quotes");

        builder.HasKey(quote => quote.Id);

        builder.Property(quote => quote.Id)
            .ValueGeneratedNever();

        builder.Property(quote => quote.Symbol)
            .HasConversion(ValueObjectConverters.Symbol)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(quote => quote.Bid)
            .HasConversion(ValueObjectConverters.Price)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(quote => quote.Ask)
            .HasConversion(ValueObjectConverters.Price)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(quote => quote.Timestamp)
            .IsRequired();

        builder.HasIndex(quote => new { quote.Symbol, quote.Timestamp })
            .IsDescending(false, true);

        builder.HasOne<Instrument>()
            .WithMany()
            .HasForeignKey(quote => quote.Symbol)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
