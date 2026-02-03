using AutoMapper;
using CashFlow.BuildingBlocks.Core.Results;
using CashFlow.Transactions.Application.DTOs;
using CashFlow.Transactions.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CashFlow.Transactions.Application.Queries.GetTransactionsByDate;

/// <summary>
/// Handler for GetTransactionsByDateQuery
/// </summary>
public sealed class GetTransactionsByDateQueryHandler
    : IRequestHandler<GetTransactionsByDateQuery, Result<IReadOnlyList<TransactionDto>>>
{
    private readonly ITransactionRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTransactionsByDateQueryHandler> _logger;

    public GetTransactionsByDateQueryHandler(
        ITransactionRepository repository,
        IMapper mapper,
        ILogger<GetTransactionsByDateQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<TransactionDto>>> Handle(
        GetTransactionsByDateQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _repository.GetByDateAsync(request.Date.Date, cancellationToken);
            var dtos = _mapper.Map<IReadOnlyList<TransactionDto>>(transactions);

            _logger.LogInformation(
                "Retrieved {Count} transactions for date {Date}",
                dtos.Count,
                request.Date.Date);

            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions for date {Date}", request.Date);
            return Result.Failure<IReadOnlyList<TransactionDto>>(
                Error.Failure("Transaction.RetrievalFailed", "An error occurred while retrieving transactions"));
        }
    }
}
