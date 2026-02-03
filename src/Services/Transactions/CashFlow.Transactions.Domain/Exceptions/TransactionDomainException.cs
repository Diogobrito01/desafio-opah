using CashFlow.BuildingBlocks.Core.Exceptions;

namespace CashFlow.Transactions.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain rule is violated in the Transaction context
/// </summary>
public sealed class TransactionDomainException : DomainException
{
    public TransactionDomainException(string message)
        : base(message)
    {
    }

    public TransactionDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
