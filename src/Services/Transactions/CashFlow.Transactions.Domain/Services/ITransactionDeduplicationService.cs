using CashFlow.Transactions.Domain.Entities;
using CashFlow.Transactions.Domain.ValueObjects;

namespace CashFlow.Transactions.Domain.Services;

/// <summary>
/// Domain Service for detecting potential duplicate transactions
/// Follows Domain-Driven Design: encapsulates business logic that doesn't naturally fit in an entity
/// </summary>
public interface ITransactionDeduplicationService
{
    /// <summary>
    /// Finds potential duplicate transactions based on business rules
    /// Rules:
    /// - Same amount and type within 5 minutes: High risk (100 points)
    /// - Same amount, type, and date: Medium risk (75 points)
    /// - Similar description (Levenshtein distance): Variable risk (50-75 points)
    /// - Same reference: High risk (90 points)
    /// </summary>
    Task<IReadOnlyList<PotentialDuplicate>> FindPotentialDuplicatesAsync(
        decimal amount,
        string type,
        string description,
        DateTime transactionDate,
        string? reference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates similarity score between two descriptions using Levenshtein distance
    /// Returns a score from 0 to 100 (100 being identical)
    /// </summary>
    int CalculateDescriptionSimilarity(string description1, string description2);
}
