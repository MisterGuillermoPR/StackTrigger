using FluentAssertions;
using NSubstitute;
using StackTrigger.Application.Metrics.Queries.GetAdminMetrics;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Enums;
using StackTrigger.Domain.Repositories;
using Xunit;

namespace StackTrigger.Application.UnitTests.Metrics;

public sealed class GetAdminMetricsQueryHandlerTests
{
    private readonly IIntentSignalRepository _signalRepoMock;
    private readonly IClientSubscriptionRepository _subRepoMock;
    private readonly GetAdminMetricsQueryHandler _handler;

    public GetAdminMetricsQueryHandlerTests()
    {
        _signalRepoMock = Substitute.For<IIntentSignalRepository>();
        _subRepoMock = Substitute.For<IClientSubscriptionRepository>();
        _handler = new GetAdminMetricsQueryHandler(_signalRepoMock, _subRepoMock);
    }

    [Fact]
    public async Task Handle_ShouldComputeMrrAndCountsAccurately()
    {
        // Arrange
        _signalRepoMock.GetCountAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(150));
        _signalRepoMock.GetCountAsync(DispatchStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(25));
        _signalRepoMock.GetCountAsync(DispatchStatus.Dispatched, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(125));

        var sub1 = ClientSubscription.Create("Cloud MSP Inc", "https://hooks.slack.com/1", new[] { "Azure" });
        var sub2 = ClientSubscription.Create("SecOps Pro", "https://hooks.slack.com/2", new[] { "SOC2" });
        var sub3 = ClientSubscription.Create("Paused Client", "https://hooks.slack.com/3", new[] { "AWS" });
        sub3.Deactivate();

        _subRepoMock.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ClientSubscription>>(new List<ClientSubscription> { sub1, sub2, sub3 }));

        var signal = IntentSignal.Create(
            "Datadog",
            "Senior Cloud Security Engineer",
            new[] { "AWS", "Terraform" },
            "https://boards.greenhouse.io/datadog/jobs/999"
        );
        _signalRepoMock.GetRecentSignalsAsync(15, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IntentSignal>>(new List<IntentSignal> { signal }));

        // Act
        var result = await _handler.Handle(new GetAdminMetricsQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalLeadsScraped.Should().Be(150);
        result.PendingLeads.Should().Be(25);
        result.DispatchedLeads.Should().Be(125);
        result.ActiveSubscribers.Should().Be(2);
        result.MonthlyRecurringRevenue.Should().Be(800.00m); // 2 active * $400
        result.Subscribers.Should().HaveCount(3);
        result.RecentLeads.Should().ContainSingle();
        result.RecentLeads[0].CompanyName.Should().Be("Datadog");
    }
}
