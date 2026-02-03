namespace CashFlow.Consolidation.Application.DTOs;

/// <summary>
/// Data Transfer Object for DailyConsolidation
/// </summary>
public sealed record DailyConsolidationDto
{
    public Guid Id { get; init; }
    public DateTime Date { get; init; }
    public decimal TotalCredits { get; init; }
    public decimal TotalDebits { get; init; }
    public decimal Balance { get; init; }
    public int TransactionCount { get; init; }
    public DateTime LastUpdated { get; init; }
}
