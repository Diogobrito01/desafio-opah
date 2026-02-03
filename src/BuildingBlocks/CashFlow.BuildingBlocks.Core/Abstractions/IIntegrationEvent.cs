namespace CashFlow.BuildingBlocks.Core.Abstractions;

/// <summary>
/// Marker interface for integration events (cross-service communication)
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
