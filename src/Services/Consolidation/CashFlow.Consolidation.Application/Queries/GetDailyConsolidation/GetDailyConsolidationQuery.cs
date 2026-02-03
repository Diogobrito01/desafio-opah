using CashFlow.BuildingBlocks.Core.Results;
using CashFlow.Consolidation.Application.DTOs;
using MediatR;

namespace CashFlow.Consolidation.Application.Queries.GetDailyConsolidation;

/// <summary>
/// Query to get daily consolidation for a specific date
/// </summary>
public sealed record GetDailyConsolidationQuery(DateTime Date) : IRequest<Result<DailyConsolidationDto>>;
