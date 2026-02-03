using CashFlow.Transactions.Domain.Entities;

namespace CashFlow.Transactions.Domain.Repositories;

/// <summary>
/// Repository interface for Transaction aggregate following Repository Pattern
/// </summary>
public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<int> CountByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}
