using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Enums;
using StackTrigger.Infrastructure.Persistence;
using StackTrigger.Infrastructure.Persistence.Repositories;

namespace StackTrigger.Domain.UnitTests.Infrastructure;

public sealed class IntentSignalRepositoryTests
{
    private static StackTriggerDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<StackTriggerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new StackTriggerDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistIntentSignal()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new IntentSignalRepository(dbContext);

        var signal = IntentSignal.Create(
            "Microsoft",
            "Azure Cloud Specialist",
            ["Azure", "Kubernetes"],
            "https://careers.microsoft.com/jobs/101"
        );

        // Act
        await repository.AddAsync(signal);
        await repository.SaveChangesAsync();

        // Assert
        var persisted = await repository.GetByIdAsync(signal.Id);
        persisted.Should().NotBeNull();
        persisted!.CompanyName.Should().Be("Microsoft");
        persisted.DispatchStatus.Should().Be(DispatchStatus.Pending);
    }

    [Fact]
    public async Task ExistsWithSourceUrlAsync_ShouldReturnTrue_WhenUrlExists()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new IntentSignalRepository(dbContext);
        const string sourceUrl = "https://careers.aws.com/jobs/999";

        var signal = IntentSignal.Create("AWS Partner", "DevOps Engineer", ["AWS", "Terraform"], sourceUrl);
        await repository.AddAsync(signal);
        await repository.SaveChangesAsync();

        // Act
        var exists = await repository.ExistsWithSourceUrlAsync(sourceUrl);
        var nonExistent = await repository.ExistsWithSourceUrlAsync("https://notfound.com/jobs/1");

        // Assert
        exists.Should().BeTrue();
        nonExistent.Should().BeFalse();
    }

    [Fact]
    public async Task GetPendingSignalsAsync_ShouldReturnOnlyPendingSignalsOrderedByDate()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new IntentSignalRepository(dbContext);

        var olderPending = IntentSignal.Create(
            "Company A", "Senior Engineer", [".NET 9"],
            "https://comp-a.com/job/1",
            DateTimeOffset.UtcNow.AddMinutes(-30)
        );

        var newerPending = IntentSignal.Create(
            "Company B", "Lead Architect", ["Azure"],
            "https://comp-b.com/job/2",
            DateTimeOffset.UtcNow.AddMinutes(-10)
        );

        var dispatched = IntentSignal.Create(
            "Company C", "Security Specialist", ["CrowdStrike"],
            "https://comp-c.com/job/3",
            DateTimeOffset.UtcNow.AddMinutes(-20)
        );
        dispatched.MarkAsDispatched();

        await repository.AddAsync(olderPending);
        await repository.AddAsync(newerPending);
        await repository.AddAsync(dispatched);
        await repository.SaveChangesAsync();

        // Act
        var pending = await repository.GetPendingSignalsAsync();

        // Assert
        pending.Should().HaveCount(2);
        pending[0].Id.Should().Be(olderPending.Id);
        pending[1].Id.Should().Be(newerPending.Id);
    }
}

