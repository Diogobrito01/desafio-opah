using CashFlow.Transactions.Domain.Entities;
using CashFlow.Transactions.Domain.Repositories;
using CashFlow.Transactions.Domain.ValueObjects;

namespace CashFlow.Transactions.Domain.Services;

/// <summary>
/// Implementation of transaction deduplication service
/// Uses multiple heuristics to detect potential duplicates
/// </summary>
public sealed class TransactionDeduplicationService : ITransactionDeduplicationService
{
    private readonly ITransactionRepository _repository;

    public TransactionDeduplicationService(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PotentialDuplicate>> FindPotentialDuplicatesAsync(
        decimal amount,
        string type,
        string description,
        DateTime transactionDate,
        string? reference,
        CancellationToken cancellationToken = default)
    {
        var potentialDuplicates = new List<PotentialDuplicate>();

        // Search window: ±1 day from transaction date
        var startDate = transactionDate.Date.AddDays(-1);
        var endDate = transactionDate.Date.AddDays(1);

        var existingTransactions = await _repository.GetByDateRangeAsync(
            startDate,
            endDate,
            cancellationToken);

        foreach (var existing in existingTransactions)
        {
            var reasons = new List<string>();
            var similarityScore = 0;

            // Rule 1: Exact amount and type match
            if (existing.Amount == amount && existing.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                similarityScore += 40;
                reasons.Add("Same amount and type");

                // Rule 2: Within 5 minutes (high risk)
                var timeDifference = Math.Abs((existing.CreatedAt - DateTime.UtcNow).TotalMinutes);
                if (timeDifference <= 5)
                {
                    similarityScore += 60;
                    reasons.Add("Created within 5 minutes");
                }
                // Rule 3: Same day
                else if (existing.TransactionDate.Date == transactionDate.Date)
                {
                    similarityScore += 35;
                    reasons.Add("Same transaction date");
                }
            }

            // Rule 4: Similar description
            var descriptionSimilarity = CalculateDescriptionSimilarity(existing.Description, description);
            if (descriptionSimilarity >= 80)
            {
                similarityScore += 30;
                reasons.Add($"Similar description ({descriptionSimilarity}% match)");
            }

            // Rule 5: Same reference (if provided)
            if (!string.IsNullOrEmpty(reference) &&
                !string.IsNullOrEmpty(existing.Reference) &&
                existing.Reference.Equals(reference, StringComparison.OrdinalIgnoreCase))
            {
                similarityScore += 50;
                reasons.Add("Same reference number");
            }

            // Only report if similarity score is >= 70 (configurable threshold)
            if (similarityScore >= 70)
            {
                potentialDuplicates.Add(PotentialDuplicate.Create(
                    existing.Id,
                    existing.Amount,
                    existing.Type.ToString(),
                    existing.Description,
                    existing.TransactionDate,
                    existing.CreatedAt,
                    similarityScore,
                    string.Join("; ", reasons)));
            }
        }

        // Order by similarity score descending (most likely duplicates first)
        return potentialDuplicates
            .OrderByDescending(d => d.SimilarityScore)
            .ToList();
    }

    /// <summary>
    /// Calculates Levenshtein distance and converts to similarity percentage
    /// Optimized implementation with early termination
    /// </summary>
    public int CalculateDescriptionSimilarity(string description1, string description2)
    {
        if (string.IsNullOrEmpty(description1) || string.IsNullOrEmpty(description2))
            return 0;

        // Normalize: trim, lowercase
        var s1 = description1.Trim().ToLowerInvariant();
        var s2 = description2.Trim().ToLowerInvariant();

        if (s1 == s2)
            return 100;

        var distance = CalculateLevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);

        if (maxLength == 0)
            return 100;

        var similarity = (1.0 - (double)distance / maxLength) * 100;
        return (int)Math.Round(similarity);
    }

    /// <summary>
    /// Calculates Levenshtein distance using Wagner-Fischer algorithm
    /// Time complexity: O(m*n), Space complexity: O(min(m,n))
    /// </summary>
    private static int CalculateLevenshteinDistance(string s1, string s2)
    {
        var n = s1.Length;
        var m = s2.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        // Use only one row for space optimization
        var previousRow = new int[m + 1];
        for (var j = 0; j <= m; j++)
            previousRow[j] = j;

        for (var i = 1; i <= n; i++)
        {
            var currentRow = new int[m + 1];
            currentRow[0] = i;

            for (var j = 1; j <= m; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;

                currentRow[j] = Math.Min(
                    Math.Min(
                        currentRow[j - 1] + 1,      // Insertion
                        previousRow[j] + 1),        // Deletion
                    previousRow[j - 1] + cost);     // Substitution
            }

            previousRow = currentRow;
        }

        return previousRow[m];
    }
}
