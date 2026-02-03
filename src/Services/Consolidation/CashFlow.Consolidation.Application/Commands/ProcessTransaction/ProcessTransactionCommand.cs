using CashFlow.BuildingBlocks.Core.Results;
using MediatR;

namespace CashFlow.Consolidation.Application.Commands.ProcessTransaction;

/// <summary>
/// Command to process a transaction event and update the daily consolidation
/// </summary>
public sealed record ProcessTransactionCommand : IRequest<Result>
{
    public Guid TransactionId { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public DateTime TransactionDate { get; init; }
}
