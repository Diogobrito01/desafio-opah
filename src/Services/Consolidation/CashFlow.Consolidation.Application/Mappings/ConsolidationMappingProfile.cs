using AutoMapper;
using CashFlow.Consolidation.Application.DTOs;
using CashFlow.Consolidation.Domain.Entities;

namespace CashFlow.Consolidation.Application.Mappings;

/// <summary>
/// AutoMapper profile for Consolidation mappings
/// </summary>
public sealed class ConsolidationMappingProfile : Profile
{
    public ConsolidationMappingProfile()
    {
        CreateMap<DailyConsolidation, DailyConsolidationDto>();
    }
}
