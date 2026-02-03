namespace CashFlow.Transactions.Domain.Repositories;

/// <summary>
/// Unit of Work pattern for managing transactions
/// Ensures atomicity of operations across multiple repositories
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
