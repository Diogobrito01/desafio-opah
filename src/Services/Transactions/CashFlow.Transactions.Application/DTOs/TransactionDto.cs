namespace CashFlow.Transactions.Application.DTOs;

/// <summary>
/// Data Transfer Object for Transaction
/// </summary>
public sealed record TransactionDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime TransactionDate { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public string? Reference { get; init; }
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// List of potential duplicate transactions detected (if any)
    /// </summary>
    public IReadOnlyList<DuplicateWarningDto>? PotentialDuplicates { get; init; }
}
