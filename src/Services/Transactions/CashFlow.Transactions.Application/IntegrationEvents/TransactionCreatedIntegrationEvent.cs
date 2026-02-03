using CashFlow.BuildingBlocks.Core.Abstractions;

namespace CashFlow.Transactions.Application.IntegrationEvents;

/// <summary>
/// Integration event published when a transaction is created
/// Used for cross-service communication with the Consolidation service
/// </summary>
public sealed class TransactionCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid TransactionId { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public DateTime TransactionDate { get; init; }
}
