namespace CashFlow.Transactions.Application.DTOs;

/// <summary>
/// DTO for potential duplicate transaction warnings
/// </summary>
public sealed record DuplicateWarningDto
{
    public Guid TransactionId { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime TransactionDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public int SimilarityScore { get; init; }
    public string Reason { get; init; } = string.Empty;
}
