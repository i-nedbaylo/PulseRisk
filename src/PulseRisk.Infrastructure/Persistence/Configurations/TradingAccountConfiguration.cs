using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseRisk.Domain.Entities;

namespace PulseRisk.Infrastructure.Persistence.Configurations;

internal sealed class TradingAccountConfiguration : IEntityTypeConfiguration<TradingAccount>
{
    public void Configure(EntityTypeBuilder<TradingAccount> builder)
    {
        builder.ToTable("trading_accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .ValueGeneratedNever();

        builder.Property(account => account.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(account => account.Currency)
            .HasConversion<string>()
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(account => account.Leverage)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(account => account.CreatedAt)
            .IsRequired();

        builder.HasIndex(account => account.ClientId);

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(account => account.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
