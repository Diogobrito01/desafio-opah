using CashFlow.Transactions.Domain.Repositories;
using CashFlow.Transactions.Infrastructure.Persistence;

namespace CashFlow.Transactions.Infrastructure.Repositories;

/// <summary>
/// Implementation of Unit of Work pattern
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly TransactionsDbContext _context;

    public UnitOfWork(TransactionsDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
