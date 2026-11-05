using Microsoft.EntityFrameworkCore;
using StackTrigger.Domain.Entities;
using StackTrigger.Infrastructure.Persistence.Configurations;

namespace StackTrigger.Infrastructure.Persistence;

public class StackTriggerDbContext : DbContext
{
    public StackTriggerDbContext(DbContextOptions<StackTriggerDbContext> options)
        : base(options)
    {
    }

    public DbSet<IntentSignal> IntentSignals => Set<IntentSignal>();
    public DbSet<ClientSubscription> ClientSubscriptions => Set<ClientSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntentSignalConfiguration).Assembly);
    }
}

