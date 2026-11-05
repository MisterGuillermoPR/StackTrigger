using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Application.Common.Options;
using StackTrigger.Application.IntentSignals.Commands.ProcessPendingSignals;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Enums;
using StackTrigger.Domain.Repositories;

namespace StackTrigger.Application.UnitTests.IntentSignals.Commands;

public sealed class ProcessPendingSignalsCommandHandlerTests
{
    private readonly IIntentSignalRepository _intentSignalRepoMock;
    private readonly IClientSubscriptionRepository _clientSubscriptionRepoMock;
    private readonly IWebhookDeliveryService _webhookDeliveryServiceMock;
    private readonly ILogger<ProcessPendingSignalsCommandHandler> _loggerMock;
    private readonly IOptions<WebhookDispatcherOptions> _optionsMock;
    private readonly ProcessPendingSignalsCommandHandler _handler;

    public ProcessPendingSignalsCommandHandlerTests()
    {
        _intentSignalRepoMock = Substitute.For<IIntentSignalRepository>();
        _clientSubscriptionRepoMock = Substitute.For<IClientSubscriptionRepository>();
        _webhookDeliveryServiceMock = Substitute.For<IWebhookDeliveryService>();
        _loggerMock = Substitute.For<ILogger<ProcessPendingSignalsCommandHandler>>();
        _optionsMock = Options.Create(new WebhookDispatcherOptions
        {
            ClientWebhookUrl = "https://hooks.slack.com/services/DEFAULT",
            BatchSize = 10
        });

        _handler = new ProcessPendingSignalsCommandHandler(
            _intentSignalRepoMock,
            _clientSubscriptionRepoMock,
            _webhookDeliveryServiceMock,
            _optionsMock,
            _loggerMock);
    }

    [Fact]
    public async Task Handle_ShouldOnlyRouteSignalsToClientsWithMatchingKeywords()
    {
        // Arrange
        // Client A cares about Azure & Cloud Migration
        var clientA = ClientSubscription.Create(
            companyName: "Azure Cloud MSP",
            webhookUrl: "https://hooks.slack.com/client-a",
            targetKeywords: ["Azure", "Cloud Migration"]
        );

        // Client B cares about Cybersecurity & SOC2
        var clientB = ClientSubscription.Create(
            companyName: "CyberSec Agency",
            webhookUrl: "https://hooks.slack.com/client-b",
            targetKeywords: ["SOC2", "CrowdStrike"]
        );

        _clientSubscriptionRepoMock.GetActiveSubscriptionsAsync(Arg.Any<CancellationToken>())
            .Returns([clientA, clientB]);

        // Signal 1 matches Client A only
        var signalAzure = IntentSignal.Create(
            companyName: "FinTech Enterprise",
            jobTitle: "Senior Architect",
            targetTechnologies: ["Azure", ".NET 9"],
            sourceUrl: "https://careers.fintech.com/jobs/1"
        );

        // Signal 2 matches Client B only
        var signalSec = IntentSignal.Create(
            companyName: "HealthLogix",
            jobTitle: "CISO",
            targetTechnologies: ["SOC2", "AWS"],
            sourceUrl: "https://careers.health.com/jobs/2"
        );

        _intentSignalRepoMock.GetPendingSignalsAsync(10, Arg.Any<CancellationToken>())
            .Returns([signalAzure, signalSec]);

        _webhookDeliveryServiceMock.DeliverSignalAsync(
            Arg.Any<IntentSignal>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new ProcessPendingSignalsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalProcessed.Should().Be(2);
        result.SuccessfullyDispatched.Should().Be(2);
        result.FailedCount.Should().Be(0);

        signalAzure.DispatchStatus.Should().Be(DispatchStatus.Dispatched);
        signalSec.DispatchStatus.Should().Be(DispatchStatus.Dispatched);

        // Verify routing accuracy: SignalAzure sent ONLY to Client A
        await _webhookDeliveryServiceMock.Received(1).DeliverSignalAsync(
            signalAzure,
            "https://hooks.slack.com/client-a",
            Arg.Any<CancellationToken>());

        await _webhookDeliveryServiceMock.DidNotReceive().DeliverSignalAsync(
            signalAzure,
            "https://hooks.slack.com/client-b",
            Arg.Any<CancellationToken>());

        // Verify routing accuracy: SignalSec sent ONLY to Client B
        await _webhookDeliveryServiceMock.Received(1).DeliverSignalAsync(
            signalSec,
            "https://hooks.slack.com/client-b",
            Arg.Any<CancellationToken>());

        await _webhookDeliveryServiceMock.DidNotReceive().DeliverSignalAsync(
            signalSec,
            "https://hooks.slack.com/client-a",
            Arg.Any<CancellationToken>());

        await _intentSignalRepoMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSignalMatchesNoClients_ShouldMarkAsDispatchedWithoutCallingWebhook()
    {
        // Arrange
        var clientA = ClientSubscription.Create("MSP", "https://hooks.slack.com/client-a", ["Azure"]);
        _clientSubscriptionRepoMock.GetActiveSubscriptionsAsync(Arg.Any<CancellationToken>())
            .Returns([clientA]);

        // Non-matching signal (e.g. Ruby on Rails)
        var signalUnmatched = IntentSignal.Create(
            "Rails Co", "Ruby Dev", ["Ruby on Rails", "Heroku"], "https://railsco.com/jobs/1"
        );

        _intentSignalRepoMock.GetPendingSignalsAsync(10, Arg.Any<CancellationToken>())
            .Returns([signalUnmatched]);

        var command = new ProcessPendingSignalsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalProcessed.Should().Be(1);
        result.SuccessfullyDispatched.Should().Be(0);
        signalUnmatched.DispatchStatus.Should().Be(DispatchStatus.Dispatched);

        await _webhookDeliveryServiceMock.DidNotReceive().DeliverSignalAsync(
            Arg.Any<IntentSignal>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await _intentSignalRepoMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
