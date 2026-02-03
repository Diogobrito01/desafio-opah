using CashFlow.BuildingBlocks.Core.Results;
using CashFlow.Transactions.Application.DTOs;
using MediatR;

namespace CashFlow.Transactions.Application.Queries.GetTransactionById;

/// <summary>
/// Query to get a transaction by ID
/// </summary>
public sealed record GetTransactionByIdQuery(Guid Id) : IRequest<Result<TransactionDto>>;
