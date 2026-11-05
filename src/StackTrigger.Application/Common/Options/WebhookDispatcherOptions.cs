namespace StackTrigger.Application.Common.Options;

public sealed class WebhookDispatcherOptions
{
    public const string SectionName = "WebhookDispatcher";

    public string ClientWebhookUrl { get; set; } = "https://hooks.slack.com/services/MOCK/CLIENT/KEY123";

    public int BatchSize { get; set; } = 50;
}

