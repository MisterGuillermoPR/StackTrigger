using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Models;
using StackTrigger.Infrastructure.Webhooks;

namespace StackTrigger.Application.UnitTests.Webhooks;

public sealed class SlackWebhookDeliveryServiceTests
{
    private readonly ILogger<SlackWebhookDeliveryService> _loggerMock;

    public SlackWebhookDeliveryServiceTests()
    {
        _loggerMock = Substitute.For<ILogger<SlackWebhookDeliveryService>>();
    }

    [Fact]
    public async Task DeliverSignalAsync_ShouldSendFormattedSlackBlockKitPayload()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var fakeHandler = new InspectingHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            capturedBody = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            };
        });

        var httpClient = new HttpClient(fakeHandler);
        var deliveryService = new SlackWebhookDeliveryService(httpClient, _loggerMock);

        var signal = IntentSignal.Create(
            "Fintech Dynamics",
            "Lead Cloud Migration Architect",
            ["Azure", "Cloud Migration", ".NET 9"],
            "https://careers.fintech.com/jobs/909"
        );

        const string targetWebhook = "https://hooks.slack.com/services/T00/B00/X00";

        // Act
        var result = await deliveryService.DeliverSignalAsync(signal, targetWebhook, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri.Should().Be(new Uri(targetWebhook));
        capturedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");

        capturedBody.Should().NotBeNull();
        using var jsonDoc = JsonDocument.Parse(capturedBody!);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("blocks", out var blocks).Should().BeTrue();
        blocks.GetArrayLength().Should().Be(9); // Header, Fields, Budget, Tech, DM, Icebreaker, Actions, Context, Divider

        // Verify company, button URL, DM, and icebreaker in payload
        capturedBody.Should().Contain("Fintech Dynamics");
        capturedBody.Should().Contain("Lead Cloud Migration Architect");
        capturedBody.Should().Contain("https://careers.fintech.com/jobs/909");
        capturedBody.Should().Contain("`Azure`");
        capturedBody.Should().Contain("linkedin.com");
    }

    [Fact]
    public async Task DeliverSignalAsync_WhenSlackReturnsError_ShouldReturnFalse()
    {
        // Arrange
        var fakeHandler = new InspectingHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("invalid_payload")
            }));

        var httpClient = new HttpClient(fakeHandler);
        var deliveryService = new SlackWebhookDeliveryService(httpClient, _loggerMock);

        var signal = IntentSignal.Create(
            "Acme", "Dev", ["AWS"], "https://acme.com/jobs/1"
        );

        // Act
        var result = await deliveryService.DeliverSignalAsync(signal, "https://hooks.slack.com/test", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeliverWeeklyDigestAsync_ShouldSendFormattedWeeklyDigestBlockKitPayload()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var fakeHandler = new InspectingHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            capturedBody = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            };
        });

        var httpClient = new HttpClient(fakeHandler);
        var deliveryService = new SlackWebhookDeliveryService(httpClient, _loggerMock);

        var digest = new WeeklyDigestSummary(
            "Apex Cloud MSP",
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow,
            18,
            new Dictionary<string, int> { { "Azure", 12 }, { "SOC2", 6 } },
            new[] { "Datadog", "Cloudflare" },
            450000.00m
        );

        const string targetWebhook = "https://hooks.slack.com/services/T99/B99/K99";

        // Act
        var result = await deliveryService.DeliverWeeklyDigestAsync(digest, targetWebhook, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri.Should().Be(new Uri(targetWebhook));

        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("StackTrigger Weekly Value Digest");
        capturedBody.Should().Contain("Apex Cloud MSP");
        capturedBody.Should().Contain("18");
        capturedBody.Should().Contain("Azure (12)");
        capturedBody.Should().Contain("Datadog");
        capturedBody.Should().Contain("$450,000");
    }

    private sealed class InspectingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public InspectingHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}

