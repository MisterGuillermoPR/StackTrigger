using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackTrigger.Domain.Entities;

namespace StackTrigger.Infrastructure.Persistence.Configurations;

public sealed class ClientSubscriptionConfiguration : IEntityTypeConfiguration<ClientSubscription>
{
    public void Configure(EntityTypeBuilder<ClientSubscription> builder)
    {
        builder.ToTable("client_subscriptions");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.CompanyName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(c => c.WebhookUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(c => c.TargetKeywords)
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("ix_client_subscriptions_is_active");
    }
}

