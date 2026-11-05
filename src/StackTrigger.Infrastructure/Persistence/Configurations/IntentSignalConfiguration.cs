using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackTrigger.Domain.Entities;

namespace StackTrigger.Infrastructure.Persistence.Configurations;

public sealed class IntentSignalConfiguration : IEntityTypeConfiguration<IntentSignal>
{
    public void Configure(EntityTypeBuilder<IntentSignal> builder)
    {
        builder.ToTable("intent_signals");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.CompanyName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(s => s.JobTitle)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(s => s.SourceUrl)
            .HasMaxLength(2048)
            .IsRequired();

        // Enforce deduplication at the DB level
        builder.HasIndex(s => s.SourceUrl)
            .IsUnique()
            .HasDatabaseName("ix_intent_signals_source_url_unique");

        builder.Property(s => s.TargetTechnologies)
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(s => s.DateDetected)
            .IsRequired();

        builder.Property(s => s.SourceDate)
            .IsRequired(false);

        builder.Property(s => s.RelevanceScore)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.DispatchStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Query performance index for dispatcher background worker
        builder.HasIndex(s => s.DispatchStatus)
            .HasDatabaseName("ix_intent_signals_dispatch_status");
    }
}

