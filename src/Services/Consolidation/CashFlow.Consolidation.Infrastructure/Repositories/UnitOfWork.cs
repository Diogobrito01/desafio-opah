using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Consolidation.Infrastructure.Persistence;

namespace CashFlow.Consolidation.Infrastructure.Repositories;

/// <summary>
/// Implementation of Unit of Work pattern
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ConsolidationDbContext _context;

    public UnitOfWork(ConsolidationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
