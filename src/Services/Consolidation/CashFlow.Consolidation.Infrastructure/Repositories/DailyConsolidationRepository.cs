using CashFlow.Consolidation.Domain.Entities;
using CashFlow.Consolidation.Domain.Repositories;
using CashFlow.Consolidation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infrastructure.Repositories;

/// <summary>
/// Implementation of IDailyConsolidationRepository using Entity Framework Core
/// </summary>
public sealed class DailyConsolidationRepository : IDailyConsolidationRepository
{
    private readonly ConsolidationDbContext _context;

    public DailyConsolidationRepository(ConsolidationDbContext context)
    {
        _context = context;
    }

    public async Task<DailyConsolidation?> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var searchDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        return await _context.DailyConsolidations
            .FirstOrDefaultAsync(c => c.Date == searchDate, cancellationToken);
    }

    public async Task<IReadOnlyList<DailyConsolidation>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var startUtc = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc);
        
        return await _context.DailyConsolidations
            .AsNoTracking()
            .Where(c => c.Date >= startUtc && c.Date <= endUtc)
            .OrderBy(c => c.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DailyConsolidation consolidation, CancellationToken cancellationToken = default)
    {
        await _context.DailyConsolidations.AddAsync(consolidation, cancellationToken);
    }

    public void Update(DailyConsolidation consolidation)
    {
        _context.DailyConsolidations.Update(consolidation);
    }
}
