using CashFlow.Consolidation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Consolidation.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for DailyConsolidation entity
/// </summary>
public sealed class DailyConsolidationConfiguration : IEntityTypeConfiguration<DailyConsolidation>
{
    public void Configure(EntityTypeBuilder<DailyConsolidation> builder)
    {
        builder.ToTable("daily_consolidations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(c => c.TotalCredits)
            .HasColumnName("total_credits")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.TotalDebits)
            .HasColumnName("total_debits")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.Balance)
            .HasColumnName("balance")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.TransactionCount)
            .HasColumnName("transaction_count")
            .IsRequired();

        builder.Property(c => c.LastUpdated)
            .HasColumnName("last_updated")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Unique index on date - one consolidation per day
        builder.HasIndex(c => c.Date)
            .IsUnique()
            .HasDatabaseName("ix_daily_consolidations_date_unique");
    }
}
