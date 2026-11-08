using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Application.Digests.Commands.SendWeeklyDigests;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Models;
using StackTrigger.Domain.Repositories;
using Xunit;

namespace StackTrigger.Application.UnitTests.Digests;

public sealed class SendWeeklyDigestsCommandHandlerTests
{
    private readonly IClientSubscriptionRepository _subRepoMock;
    private readonly IIntentSignalRepository _signalRepoMock;
    private readonly IWebhookDeliveryService _webhookMock;
    private readonly ILogger<SendWeeklyDigestsCommandHandler> _loggerMock;
    private readonly SendWeeklyDigestsCommandHandler _handler;

    public SendWeeklyDigestsCommandHandlerTests()
    {
        _subRepoMock = Substitute.For<IClientSubscriptionRepository>();
        _signalRepoMock = Substitute.For<IIntentSignalRepository>();
        _webhookMock = Substitute.For<IWebhookDeliveryService>();
        _loggerMock = Substitute.For<ILogger<SendWeeklyDigestsCommandHandler>>();

        _handler = new SendWeeklyDigestsCommandHandler(
            _subRepoMock,
            _signalRepoMock,
            _webhookMock,
            _loggerMock
        );
    }

    [Fact]
    public async Task Handle_WhenActiveSubscriptionsExist_ShouldFilterMatchesAndDeliverDigests()
    {
        // Arrange
        var sub1 = ClientSubscription.Create("SkyHigh Cloud MSP", "https://hooks.slack.com/services/sub1", new[] { "Azure", "Kubernetes" });
        var sub2 = ClientSubscription.Create("SecOps Pro Agency", "https://hooks.slack.com/services/sub2", new[] { "SOC2", "CrowdStrike" });

        _subRepoMock.GetActiveSubscriptionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ClientSubscription>>(new[] { sub1, sub2 }));

        var signal1 = IntentSignal.Create("Datadog", "Azure Lead", new[] { "Azure", "Terraform" }, "https://datadog.com/job1");
        var signal2 = IntentSignal.Create("Cloudflare", "Kubernetes Architect", new[] { "Kubernetes", "Azure" }, "https://cloudflare.com/job2");
        var signal3 = IntentSignal.Create("Palantir", "Security Engineer", new[] { "SOC2", "CrowdStrike" }, "https://palantir.com/job3");

        _signalRepoMock.GetSignalsForPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IntentSignal>>(new[] { signal1, signal2, signal3 }));

        _webhookMock.DeliverWeeklyDigestAsync(Arg.Any<WeeklyDigestSummary>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var command = new SendWeeklyDigestsCommand(LookbackDays: 7);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalProcessed.Should().Be(2);
        result.TotalDelivered.Should().Be(2);
        result.TotalFailed.Should().Be(0);
        result.DeliveredToFirms.Should().Contain("SkyHigh Cloud MSP");
        result.DeliveredToFirms.Should().Contain("SecOps Pro Agency");

        // Verify sub1 received digest with 2 matching signals (signal1, signal2)
        await _webhookMock.Received(1).DeliverWeeklyDigestAsync(
            Arg.Is<WeeklyDigestSummary>(d =>
                d.CompanyName == "SkyHigh Cloud MSP" &&
                d.TotalSignalsDelivered == 2 &&
                d.TopTechnologies.ContainsKey("Azure") &&
                d.TopTargetCompanies.Contains("Datadog") &&
                d.TopTargetCompanies.Contains("Cloudflare")),
            "https://hooks.slack.com/services/sub1",
            Arg.Any<CancellationToken>()
        );

        // Verify sub2 received digest with 1 matching signal (signal3)
        await _webhookMock.Received(1).DeliverWeeklyDigestAsync(
            Arg.Is<WeeklyDigestSummary>(d =>
                d.CompanyName == "SecOps Pro Agency" &&
                d.TotalSignalsDelivered == 1 &&
                d.TopTechnologies.ContainsKey("SOC2") &&
                d.TopTargetCompanies.Contains("Palantir")),
            "https://hooks.slack.com/services/sub2",
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Handle_WhenTargetSubscriptionIdProvided_ShouldDeliverOnlyToThatClient()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var sub = ClientSubscription.Create("Target MSP", "https://hooks.slack.com/services/target", new[] { "Azure" });

        _subRepoMock.GetByIdAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ClientSubscription?>(sub));

        _signalRepoMock.GetSignalsForPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IntentSignal>>(Array.Empty<IntentSignal>()));

        _webhookMock.DeliverWeeklyDigestAsync(Arg.Any<WeeklyDigestSummary>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var command = new SendWeeklyDigestsCommand(TargetSubscriptionId: targetId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalProcessed.Should().Be(1);
        result.TotalDelivered.Should().Be(1);
        result.DeliveredToFirms.Should().ContainSingle().Which.Should().Be("Target MSP");
    }
}
