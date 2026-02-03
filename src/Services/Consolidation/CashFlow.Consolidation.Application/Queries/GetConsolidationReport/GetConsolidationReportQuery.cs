using CashFlow.BuildingBlocks.Core.Results;
using CashFlow.Consolidation.Application.DTOs;
using MediatR;

namespace CashFlow.Consolidation.Application.Queries.GetConsolidationReport;

/// <summary>
/// Query to get consolidation report for a date range
/// </summary>
public sealed record GetConsolidationReportQuery(DateTime StartDate, DateTime EndDate)
    : IRequest<Result<IReadOnlyList<DailyConsolidationDto>>>;
