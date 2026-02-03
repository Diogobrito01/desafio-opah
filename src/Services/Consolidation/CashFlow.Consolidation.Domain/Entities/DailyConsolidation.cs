using CashFlow.BuildingBlocks.Core.Abstractions;
using CashFlow.Consolidation.Domain.Exceptions;

namespace CashFlow.Consolidation.Domain.Entities;

/// <summary>
/// Represents the consolidated balance for a specific date
/// Aggregate Root following DDD principles
/// </summary>
public sealed class DailyConsolidation : Entity<Guid>, IAggregateRoot
{
    private DailyConsolidation(
        Guid id,
        DateTime date,
        decimal totalCredits,
        decimal totalDebits,
        decimal balance,
        int transactionCount)
        : base(id)
    {
        Date = date.Date;
        TotalCredits = totalCredits;
        TotalDebits = totalDebits;
        Balance = balance;
        TransactionCount = transactionCount;
        LastUpdated = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    // Required for EF Core
    private DailyConsolidation() : base(Guid.Empty)
    {
    }

    public DateTime Date { get; private set; }
    public decimal TotalCredits { get; private set; }
    public decimal TotalDebits { get; private set; }
    public decimal Balance { get; private set; }
    public int TransactionCount { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Factory method to create a new daily consolidation
    /// </summary>
    public static DailyConsolidation Create(DateTime date)
    {
        return new DailyConsolidation(
            Guid.NewGuid(),
            date.Date,
            0,
            0,
            0,
            0);
    }

    /// <summary>
    /// Adds a credit transaction to the consolidation
    /// </summary>
    public void AddCredit(decimal amount)
    {
        ValidateAmount(amount);
        
        TotalCredits += amount;
        Balance += amount;
        TransactionCount++;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a debit transaction to the consolidation
    /// </summary>
    public void AddDebit(decimal amount)
    {
        ValidateAmount(amount);
        
        TotalDebits += amount;
        Balance -= amount;
        TransactionCount++;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalculates the consolidation from scratch
    /// </summary>
    public void Recalculate(decimal totalCredits, decimal totalDebits, int transactionCount)
    {
        ValidateAmount(totalCredits);
        ValidateAmount(totalDebits);

        if (transactionCount < 0)
        {
            throw new ConsolidationDomainException("Transaction count cannot be negative");
        }

        TotalCredits = totalCredits;
        TotalDebits = totalDebits;
        Balance = totalCredits - totalDebits;
        TransactionCount = transactionCount;
        LastUpdated = DateTime.UtcNow;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount < 0)
        {
            throw new ConsolidationDomainException("Amount cannot be negative");
        }
    }
}
