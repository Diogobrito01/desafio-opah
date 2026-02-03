using AutoMapper;
using CashFlow.Transactions.Application.DTOs;
using CashFlow.Transactions.Domain.Entities;

namespace CashFlow.Transactions.Application.Mappings;

/// <summary>
/// AutoMapper profile for Transaction mappings
/// </summary>
public sealed class TransactionMappingProfile : Profile
{
    public TransactionMappingProfile()
    {
        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.PotentialDuplicates, opt => opt.Ignore()); // Set manually in handler
    }
}
