namespace CashFlow.Transactions.Domain.Enums;

/// <summary>
/// Represents the type of a financial transaction
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Money coming in (positive impact on balance)
    /// </summary>
    Credit = 1,

    /// <summary>
    /// Money going out (negative impact on balance)
    /// </summary>
    Debit = 2
}
