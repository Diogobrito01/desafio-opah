using CashFlow.BuildingBlocks.Core.Results;
using CashFlow.Transactions.Application.DTOs;
using MediatR;

namespace CashFlow.Transactions.Application.Queries.GetTransactionsByDate;

/// <summary>
/// Query to get all transactions for a specific date
/// </summary>
public sealed record GetTransactionsByDateQuery(DateTime Date) : IRequest<Result<IReadOnlyList<TransactionDto>>>;
