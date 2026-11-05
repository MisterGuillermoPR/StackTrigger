using StackTrigger.Domain.Entities;

namespace StackTrigger.Domain.Repositories;

public interface IClientSubscriptionRepository
{
    Task<ClientSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientSubscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientSubscription>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ClientSubscription subscription, CancellationToken cancellationToken = default);
    Task UpdateAsync(ClientSubscription subscription, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

