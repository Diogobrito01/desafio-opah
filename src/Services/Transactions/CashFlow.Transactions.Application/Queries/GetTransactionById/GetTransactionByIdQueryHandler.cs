using AutoMapper;
using CashFlow.BuildingBlocks.Core.Results;
using CashFlow.Transactions.Application.DTOs;
using CashFlow.Transactions.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CashFlow.Transactions.Application.Queries.GetTransactionById;

/// <summary>
/// Handler for GetTransactionByIdQuery with caching support
/// </summary>
public sealed class GetTransactionByIdQueryHandler
    : IRequestHandler<GetTransactionByIdQuery, Result<TransactionDto>>
{
    private readonly ITransactionRepository _repository;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GetTransactionByIdQueryHandler> _logger;

    public GetTransactionByIdQueryHandler(
        ITransactionRepository repository,
        IMapper mapper,
        IDistributedCache cache,
        ILogger<GetTransactionByIdQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<TransactionDto>> Handle(
        GetTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Try to get from cache first
            var cacheKey = $"transaction:{request.Id}";
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("Transaction {TransactionId} retrieved from cache", request.Id);
                var cachedDto = JsonSerializer.Deserialize<TransactionDto>(cachedData);
                return Result.Success(cachedDto!);
            }

            // Get from database
            var transaction = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (transaction is null)
            {
                return Result.Failure<TransactionDto>(
                    Error.NotFound("Transaction.NotFound", $"Transaction with ID {request.Id} not found"));
            }

            var dto = _mapper.Map<TransactionDto>(transaction);

            // Cache the result
            var serializedDto = JsonSerializer.Serialize(dto);
            await _cache.SetStringAsync(
                cacheKey,
                serializedDto,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                },
                cancellationToken);

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction {TransactionId}", request.Id);
            return Result.Failure<TransactionDto>(
                Error.Failure("Transaction.RetrievalFailed", "An error occurred while retrieving the transaction"));
        }
    }
}
