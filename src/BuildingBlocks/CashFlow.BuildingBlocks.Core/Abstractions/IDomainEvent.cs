using MediatR;

namespace CashFlow.BuildingBlocks.Core.Abstractions;

/// <summary>
/// Marker interface for domain events
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
