using AutoMapper;
using CashFlow.BuildingBlocks.Core.Results;
using CashFlow.Consolidation.Application.DTOs;
using CashFlow.Consolidation.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Application.Queries.GetConsolidationReport;

/// <summary>
/// Handler for GetConsolidationReportQuery
/// </summary>
public sealed class GetConsolidationReportQueryHandler
    : IRequestHandler<GetConsolidationReportQuery, Result<IReadOnlyList<DailyConsolidationDto>>>
{
    private readonly IDailyConsolidationRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetConsolidationReportQueryHandler> _logger;

    public GetConsolidationReportQueryHandler(
        IDailyConsolidationRepository repository,
        IMapper mapper,
        ILogger<GetConsolidationReportQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<DailyConsolidationDto>>> Handle(
        GetConsolidationReportQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.StartDate > request.EndDate)
            {
                return Result.Failure<IReadOnlyList<DailyConsolidationDto>>(
                    Error.Validation("Consolidation.InvalidDateRange", "Start date must be before or equal to end date"));
            }

            var consolidations = await _repository.GetByDateRangeAsync(
                request.StartDate.Date,
                request.EndDate.Date,
                cancellationToken);

            var dtos = _mapper.Map<IReadOnlyList<DailyConsolidationDto>>(consolidations);

            _logger.LogInformation(
                "Retrieved {Count} consolidations from {StartDate} to {EndDate}",
                dtos.Count,
                request.StartDate.Date,
                request.EndDate.Date);

            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving consolidation report");
            return Result.Failure<IReadOnlyList<DailyConsolidationDto>>(
                Error.Failure("Consolidation.ReportFailed", "An error occurred while generating the report"));
        }
    }
}
