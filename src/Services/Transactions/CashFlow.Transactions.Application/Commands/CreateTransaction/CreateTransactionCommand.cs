using CashFlow.BuildingBlocks.Core.Results;
using CashFlow.Transactions.Application.DTOs;
using MediatR;

namespace CashFlow.Transactions.Application.Commands.CreateTransaction;

/// <summary>
/// Command to create a new transaction
/// Follows CQRS pattern and includes idempotency key for duplicate prevention
/// </summary>
public sealed record CreateTransactionCommand : IRequest<Result<TransactionResponseDto>>
{
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime TransactionDate { get; init; }
    
    /// <summary>
    /// Idempotency key to prevent duplicate transactions
    /// Must be unique per transaction and at least 16 characters
    /// Recommended: Use GUID, UUID, or combination of client-id + timestamp + nonce
    /// Example: "client-123-20260203120000-abc123"
    /// </summary>
    public string IdempotencyKey { get; init; } = string.Empty;
    
    public string? Reference { get; init; }
}
