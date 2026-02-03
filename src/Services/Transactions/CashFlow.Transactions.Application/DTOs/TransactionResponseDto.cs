namespace CashFlow.Transactions.Application.DTOs;

/// <summary>
/// Response DTO for transaction creation with metadata about idempotency
/// </summary>
public sealed record TransactionResponseDto
{
    public TransactionDto Transaction { get; init; } = null!;
    
    /// <summary>
    /// Indicates whether this is a newly created transaction (true) 
    /// or an existing transaction returned due to idempotency (false)
    /// </summary>
    public bool IsNewTransaction { get; init; }
    
    /// <summary>
    /// Message indicating the result of the operation
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
