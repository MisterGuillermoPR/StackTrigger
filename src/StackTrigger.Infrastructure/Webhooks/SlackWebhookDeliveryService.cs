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
                    "Successfully dispatched intent signal {SignalId} to Slack webhook for {Company}",
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

        return new
        {
            blocks = new object[]
            {
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
                new
                {
                    type = "section",
                    fields = new object[]
                    {
                        new
                        {
                            type = "mrkdwn",
                            text = $"*Company:*\n{signal.CompanyName}"
                        },
                        new
                        {
                            type = "mrkdwn",
                            text = $"*Target Role:*\n{signal.JobTitle}"
                        }
                    }
                },
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = $"*Detected Tech Migration / Stack Signals:*\n{techBadges}"
                    }
                },
                new
                {
                    type = "context",
                    elements = new object[]
                    {
                        new
                        {
                            type = "mrkdwn",
                            text = $"*Detected at:* <!date^{signal.DateDetected.ToUnixTimeSeconds()}^{{date_num}} {{time_secs}}|{signal.DateDetected:yyyy-MM-dd HH:mm:ss UTC}>  •  *Via:* StackTrigger Autonomous Engine"
                        }
                    }
                },
                new
                {
                    type = "actions",
                    elements = new object[]
                    {
                        new
                        {
                            type = "button",
                            text = new
                            {
                                type = "plain_text",
                                text = "View Source Job Posting",
                                emoji = true
                            },
                            style = "primary",
                            url = signal.SourceUrl
                        }
                    }
                },
                new
                {
                    type = "divider"
                }
            }
        };
    }
}
