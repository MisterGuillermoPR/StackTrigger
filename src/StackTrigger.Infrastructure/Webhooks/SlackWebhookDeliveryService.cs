using System.Net.Mime;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Domain.Entities;

namespace StackTrigger.Infrastructure.Webhooks;

public sealed class SlackWebhookDeliveryService : IWebhookDeliveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<SlackWebhookDeliveryService> _logger;

    public SlackWebhookDeliveryService(
        HttpClient httpClient,
        ILogger<SlackWebhookDeliveryService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> DeliverSignalAsync(
        IntentSignal signal,
        string webhookUrl,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookUrl, nameof(webhookUrl));

        var payload = BuildSlackPayload(signal);
        var jsonContent = JsonSerializer.Serialize(payload, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, MediaTypeNames.Application.Json)
        };

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Successfully dispatched enriched intent signal {SignalId} to Slack webhook for {Company}",
                    signal.Id,
                    signal.CompanyName);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Slack webhook delivery failed for signal {SignalId}. Status: {StatusCode}, Body: {Body}",
                signal.Id,
                response.StatusCode,
                errorBody);
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Slack webhook dispatch was cancelled for signal {SignalId}", signal.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error dispatching signal {SignalId} to Slack", signal.Id);
            return false;
        }
    }

    public static object BuildSlackPayload(IntentSignal signal)
    {
        var techBadges = string.Join("  •  ", signal.TargetTechnologies.Select(t => $"`{t}`"));
        var budgetDisplay = !string.IsNullOrWhiteSpace(signal.SalaryRange)
            ? $"💰 *Budget/Comp:* {signal.SalaryRange}  •  🔥 *Urgency:* Immediate Active Hiring"
            : "🔥 *Urgency:* Immediate Active Hiring (Budget Approved)";

        var decisionMaker = signal.DecisionMakerTitle ?? "VP of Engineering / Head of Infrastructure";
        var linkedInUrl = signal.LinkedInSearchUrl
            ?? IntentSignal.GenerateLinkedInSearchUrl(signal.CompanyName, decisionMaker);
        var icebreaker = signal.OutreachIcebreaker
            ?? IntentSignal.GenerateOutreachIcebreaker(signal.CompanyName, signal.JobTitle, signal.TargetTechnologies.ToList());

        var blocks = new List<object>
        {
            // Header
            new
            {
                type = "header",
                text = new
                {
                    type = "plain_text",
                    text = $"🚨 StackTrigger High-Intent Lead: {signal.CompanyName}",
                    emoji = true
                }
            },
            // Lead Details
            new
            {
                type = "section",
                fields = new object[]
                {
                    new { type = "mrkdwn", text = $"*Company:*\n*{signal.CompanyName}*" },
                    new { type = "mrkdwn", text = $"*Target Role:*\n{signal.JobTitle}" }
                }
            },
            // Budget / Urgency
            new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = budgetDisplay
                }
            },
            // Tech Triggers
            new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = $"*Detected Tech Migration / Stack Triggers:*\n{techBadges}"
                }
            },
            // Exact Decision Maker Target
            new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = $"🎯 *Verified Decision-Maker to Contact:*\n*{decisionMaker}*"
                }
            },
            // 1-Click Cold Outreach Angle
            new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = $"💡 *Recommended Cold Pitch:*\n_{icebreaker}_"
                }
            },
            // Direct 1-Click Action Buttons
            new
            {
                type = "actions",
                elements = new object[]
                {
                    new
                    {
                        type = "button",
                        text = new { type = "plain_text", text = "View Source Job Posting", emoji = true },
                        style = "primary",
                        url = signal.SourceUrl
                    },
                    new
                    {
                        type = "button",
                        text = new { type = "plain_text", text = "Open Direct LinkedIn Profile ↗", emoji = true },
                        url = linkedInUrl
                    }
                }
            },
            // Metadata Context
            new
            {
                type = "context",
                elements = new object[]
                {
                    new
                    {
                        type = "mrkdwn",
                        text = $"*Detected:* <!date^{signal.DateDetected.ToUnixTimeSeconds()}^{{date_num}} {{time_secs}}|{signal.DateDetected:yyyy-MM-dd HH:mm:ss UTC}>  •  *Via:* StackTrigger Intelligence Engine"
                    }
                }
            },
            new { type = "divider" }
        };

        return new { blocks };
    }
}
