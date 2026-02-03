namespace CashFlow.Transactions.Domain.ValueObjects;

/// <summary>
/// Value object representing a potential duplicate transaction
/// Immutable by design following DDD principles
/// </summary>
public sealed record PotentialDuplicate
{
    public Guid TransactionId { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime TransactionDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public int SimilarityScore { get; init; }
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Creates a potential duplicate with a similarity score and reason
    /// </summary>
    public static PotentialDuplicate Create(
        Guid transactionId,
        decimal amount,
        string type,
        string description,
        DateTime transactionDate,
        DateTime createdAt,
        int similarityScore,
        string reason)
    {
        return new PotentialDuplicate
        {
            TransactionId = transactionId,
            Amount = amount,
            Type = type,
            Description = description,
            TransactionDate = transactionDate,
            CreatedAt = createdAt,
            SimilarityScore = similarityScore,
            Reason = reason
        };
    }
}
