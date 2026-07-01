using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseRisk.Domain.Entities;

namespace PulseRisk.Infrastructure.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(client => client.Id);

        builder.Property(client => client.Id)
            .ValueGeneratedNever();

        builder.Property(client => client.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(client => client.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(client => client.CreatedAt)
            .IsRequired();
    }
}

