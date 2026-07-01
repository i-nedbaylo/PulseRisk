using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseRisk.Domain.Entities;
using PulseRisk.Infrastructure.Persistence.Converters;

namespace PulseRisk.Infrastructure.Persistence.Configurations;

internal sealed class RiskAlertConfiguration : IEntityTypeConfiguration<RiskAlert>
{
    public void Configure(EntityTypeBuilder<RiskAlert> builder)
    {
        builder.ToTable("risk_alerts");

        builder.HasKey(alert => alert.Id);

        builder.Property(alert => alert.Id)
            .ValueGeneratedNever();

        builder.Property(alert => alert.Symbol)
            .HasConversion(ValueObjectConverters.Symbol)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(alert => alert.AlertType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(alert => alert.Severity)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(alert => alert.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(alert => alert.CreatedAt)
            .IsRequired();

        builder.Property(alert => alert.ResolvedAt);

        builder.Ignore(alert => alert.IsActive);

        builder.HasIndex(alert => new { alert.ClientId, alert.CreatedAt })
            .IsDescending(false, true);

        builder.HasIndex(alert => new { alert.Severity, alert.CreatedAt })
            .IsDescending(false, true);

        builder.HasIndex(alert => alert.ResolvedAt);

        builder.HasIndex(alert => new { alert.ClientId, alert.TradingAccountId, alert.Symbol, alert.AlertType })
            .HasFilter("resolved_at IS NULL");

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(alert => alert.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TradingAccount>()
            .WithMany()
            .HasForeignKey(alert => alert.TradingAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Instrument>()
            .WithMany()
            .HasForeignKey(alert => alert.Symbol)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
