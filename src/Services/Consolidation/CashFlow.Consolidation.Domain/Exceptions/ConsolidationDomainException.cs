using CashFlow.BuildingBlocks.Core.Exceptions;

namespace CashFlow.Consolidation.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain rule is violated in the Consolidation context
/// </summary>
public sealed class ConsolidationDomainException : DomainException
{
    public ConsolidationDomainException(string message)
        : base(message)
    {
    }

    public ConsolidationDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
