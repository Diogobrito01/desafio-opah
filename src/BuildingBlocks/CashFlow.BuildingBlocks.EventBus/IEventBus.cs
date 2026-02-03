using CashFlow.BuildingBlocks.Core.Abstractions;

namespace CashFlow.BuildingBlocks.EventBus;

/// <summary>
/// Event bus abstraction for publishing integration events
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
