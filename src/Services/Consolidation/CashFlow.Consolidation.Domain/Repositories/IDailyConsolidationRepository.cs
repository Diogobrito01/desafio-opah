using CashFlow.Consolidation.Domain.Entities;

namespace CashFlow.Consolidation.Domain.Repositories;

/// <summary>
/// Repository interface for DailyConsolidation aggregate
/// </summary>
public interface IDailyConsolidationRepository
{
    Task<DailyConsolidation?> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyConsolidation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task AddAsync(DailyConsolidation consolidation, CancellationToken cancellationToken = default);
    void Update(DailyConsolidation consolidation);
}
