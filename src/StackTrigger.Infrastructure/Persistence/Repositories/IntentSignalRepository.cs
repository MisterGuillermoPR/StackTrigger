using Microsoft.EntityFrameworkCore;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Enums;
using StackTrigger.Domain.Repositories;

namespace StackTrigger.Infrastructure.Persistence.Repositories;

public sealed class IntentSignalRepository : IIntentSignalRepository
{
    private readonly StackTriggerDbContext _dbContext;

    public IntentSignalRepository(StackTriggerDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IntentSignal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IntentSignals
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IntentSignal?> GetBySourceUrlAsync(string sourceUrl, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IntentSignals
            .FirstOrDefaultAsync(s => s.SourceUrl == sourceUrl, cancellationToken);
    }

    public async Task<bool> ExistsWithSourceUrlAsync(string sourceUrl, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IntentSignals
            .AnyAsync(s => s.SourceUrl == sourceUrl, cancellationToken);
    }

    public async Task<IReadOnlyList<IntentSignal>> GetPendingSignalsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IntentSignals
            .Where(s => s.DispatchStatus == DispatchStatus.Pending)
            .OrderBy(s => s.DateDetected)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IntentSignal>> GetByStatusAsync(DispatchStatus status, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IntentSignals
            .Where(s => s.DispatchStatus == status)
            .OrderBy(s => s.DateDetected)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IntentSignal>> GetRecentSignalsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IntentSignals
            .OrderByDescending(s => s.DateDetected)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IntentSignal>> GetSignalsForPeriodAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.IntentSignals
            .Where(s => s.DateDetected >= fromDate && s.DateDetected <= toDate)
            .OrderByDescending(s => s.DateDetected)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(DispatchStatus? status = null, CancellationToken cancellationToken = default)
    {
        if (status.HasValue)
        {
            return await _dbContext.IntentSignals
                .CountAsync(s => s.DispatchStatus == status.Value, cancellationToken);
        }

        return await _dbContext.IntentSignals.CountAsync(cancellationToken);
    }

    public async Task AddAsync(IntentSignal intentSignal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intentSignal, nameof(intentSignal));
        await _dbContext.IntentSignals.AddAsync(intentSignal, cancellationToken);
    }

    public Task UpdateAsync(IntentSignal intentSignal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intentSignal, nameof(intentSignal));
        _dbContext.IntentSignals.Update(intentSignal);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

