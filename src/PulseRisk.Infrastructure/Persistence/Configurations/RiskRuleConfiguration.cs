using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;

namespace PulseRisk.Infrastructure.Persistence.Configurations;

internal sealed class RiskRuleConfiguration : IEntityTypeConfiguration<RiskRule>
{
    public void Configure(EntityTypeBuilder<RiskRule> builder)
    {
        builder.ToTable("risk_rules");

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Id)
            .ValueGeneratedNever();

        builder.Property(rule => rule.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(rule => rule.RuleType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(rule => rule.ThresholdValue)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(rule => rule.Severity)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(rule => rule.IsEnabled)
            .IsRequired();

        builder.HasIndex(rule => new { rule.RuleType, rule.IsEnabled });

        builder.HasData(
            new RiskRule(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Max exposure limit", RiskRuleType.MaxExposureLimit, 1000000m, RiskSeverity.Critical, isEnabled: true),
            new RiskRule(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Max loss limit", RiskRuleType.MaxLossLimit, -25000m, RiskSeverity.Critical, isEnabled: true),
            new RiskRule(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Margin level warning", RiskRuleType.MarginLevelWarning, 100m, RiskSeverity.Warning, isEnabled: true),
            new RiskRule(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Price spike detection", RiskRuleType.PriceSpikeDetection, 2.5m, RiskSeverity.Warning, isEnabled: true),
            new RiskRule(Guid.Parse("10000000-0000-0000-0000-000000000005"), "High frequency trading activity", RiskRuleType.HighFrequencyTradingActivity, 100m, RiskSeverity.Warning, isEnabled: true));
    }
}

