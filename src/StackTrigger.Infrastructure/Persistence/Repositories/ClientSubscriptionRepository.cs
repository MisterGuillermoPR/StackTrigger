using Microsoft.EntityFrameworkCore;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Repositories;

namespace StackTrigger.Infrastructure.Persistence.Repositories;

public sealed class ClientSubscriptionRepository : IClientSubscriptionRepository
{
    private readonly StackTriggerDbContext _dbContext;

    public ClientSubscriptionRepository(StackTriggerDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ClientSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientSubscriptions
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ClientSubscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientSubscriptions
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClientSubscription>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientSubscriptions
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ClientSubscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
        await _dbContext.ClientSubscriptions.AddAsync(subscription, cancellationToken);
    }

    public Task UpdateAsync(ClientSubscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
        _dbContext.ClientSubscriptions.Update(subscription);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

