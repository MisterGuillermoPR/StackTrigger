using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Enums;

namespace StackTrigger.Domain.Repositories;

public interface IIntentSignalRepository
{
    Task<IntentSignal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IntentSignal?> GetBySourceUrlAsync(string sourceUrl, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithSourceUrlAsync(string sourceUrl, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IntentSignal>> GetPendingSignalsAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IntentSignal>> GetByStatusAsync(DispatchStatus status, int limit = 50, CancellationToken cancellationToken = default);
    Task AddAsync(IntentSignal intentSignal, CancellationToken cancellationToken = default);
    Task UpdateAsync(IntentSignal intentSignal, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

