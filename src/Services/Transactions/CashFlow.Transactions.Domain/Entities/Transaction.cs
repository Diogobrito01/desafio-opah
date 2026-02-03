using CashFlow.BuildingBlocks.Core.Abstractions;
using CashFlow.Transactions.Domain.Enums;
using CashFlow.Transactions.Domain.Exceptions;

namespace CashFlow.Transactions.Domain.Entities;

/// <summary>
/// Represents a financial transaction (debit or credit)
/// Aggregate Root following DDD principles
/// </summary>
public sealed class Transaction : Entity<Guid>, IAggregateRoot
{
    private Transaction(
        Guid id,
        decimal amount,
        TransactionType type,
        string description,
        DateTime transactionDate,
        string idempotencyKey,
        string? reference)
        : base(id)
    {
        Amount = amount;
        Type = type;
        Description = description;
        // Ensure TransactionDate is always UTC for PostgreSQL compatibility
        TransactionDate = transactionDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(transactionDate, DateTimeKind.Utc)
            : transactionDate.ToUniversalTime();
        IdempotencyKey = idempotencyKey;
        Reference = reference;
        CreatedAt = DateTime.UtcNow;
    }

    // Required for EF Core
    private Transaction() : base(Guid.Empty)
    {
    }

    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime TransactionDate { get; private set; }
    
    /// <summary>
    /// Unique idempotency key to prevent duplicate transactions
    /// This key ensures that the same transaction cannot be created twice
    /// </summary>
    public string IdempotencyKey { get; private set; } = string.Empty;
    
    public string? Reference { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Factory method to create a new transaction with validation
    /// </summary>
    public static Transaction Create(
        decimal amount,
        TransactionType type,
        string description,
        DateTime transactionDate,
        string idempotencyKey,
        string? reference = null)
    {
        ValidateAmount(amount);
        ValidateDescription(description);
        ValidateIdempotencyKey(idempotencyKey);

        return new Transaction(
            Guid.NewGuid(),
            amount,
            type,
            description,
            transactionDate,
            idempotencyKey,
            reference);
    }

    /// <summary>
    /// Gets the signed amount based on transaction type
    /// Credits are positive, debits are negative
    /// </summary>
    public decimal GetSignedAmount()
    {
        return Type == TransactionType.Credit ? Amount : -Amount;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new TransactionDomainException("Transaction amount must be greater than zero");
        }

        if (amount > 999999999.99m)
        {
            throw new TransactionDomainException("Transaction amount exceeds maximum allowed value");
        }
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new TransactionDomainException("Transaction description is required");
        }

        if (description.Length > 500)
        {
            throw new TransactionDomainException("Transaction description cannot exceed 500 characters");
        }
    }

    private static void ValidateIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new TransactionDomainException("Idempotency key is required");
        }

        if (idempotencyKey.Length < 16)
        {
            throw new TransactionDomainException("Idempotency key must be at least 16 characters");
        }

        if (idempotencyKey.Length > 100)
        {
            throw new TransactionDomainException("Idempotency key cannot exceed 100 characters");
        }
    }
}
