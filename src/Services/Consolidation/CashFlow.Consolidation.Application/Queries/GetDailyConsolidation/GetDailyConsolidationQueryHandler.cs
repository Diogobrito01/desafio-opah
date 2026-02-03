using System.Text.Json;
using AutoMapper;
using CashFlow.BuildingBlocks.Core.Results;
using CashFlow.Consolidation.Application.DTOs;
using CashFlow.Consolidation.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Application.Queries.GetDailyConsolidation;

/// <summary>
/// Handler for GetDailyConsolidationQuery with caching support
/// </summary>
public sealed class GetDailyConsolidationQueryHandler
    : IRequestHandler<GetDailyConsolidationQuery, Result<DailyConsolidationDto>>
{
    private readonly IDailyConsolidationRepository _repository;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GetDailyConsolidationQueryHandler> _logger;

    public GetDailyConsolidationQueryHandler(
        IDailyConsolidationRepository repository,
        IMapper mapper,
        IDistributedCache cache,
        ILogger<GetDailyConsolidationQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<DailyConsolidationDto>> Handle(
        GetDailyConsolidationQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var date = request.Date.Date;
            var cacheKey = $"consolidation:{date:yyyy-MM-dd}";

            // Try to get from cache first
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("Consolidation for {Date} retrieved from cache", date);
                var cachedDto = JsonSerializer.Deserialize<DailyConsolidationDto>(cachedData);
                return Result.Success(cachedDto!);
            }

            // Get from database
            var consolidation = await _repository.GetByDateAsync(date, cancellationToken);

            if (consolidation is null)
            {
                return Result.Failure<DailyConsolidationDto>(
                    Error.NotFound("Consolidation.NotFound", $"No consolidation found for date {date:yyyy-MM-dd}"));
            }

            var dto = _mapper.Map<DailyConsolidationDto>(consolidation);

            // Cache the result
            var serializedDto = JsonSerializer.Serialize(dto);
            await _cache.SetStringAsync(
                cacheKey,
                serializedDto,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                },
                cancellationToken);

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving consolidation for date {Date}", request.Date);
            return Result.Failure<DailyConsolidationDto>(
                Error.Failure("Consolidation.RetrievalFailed", "An error occurred while retrieving the consolidation"));
        }
    }
}
