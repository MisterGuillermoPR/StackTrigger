using Microsoft.Extensions.Logging;
using StackTrigger.Domain.Entities;
using StackTrigger.Infrastructure.Webhooks;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("================================================================================");
Console.WriteLine("             STACKTRIGGER LIVE SLACK WEBHOOK DISPATCH VERIFIER                  ");
Console.WriteLine("================================================================================");
Console.ResetColor();

string webhookUrl = args.Length > 0 ? args[0] : string.Empty;

if (string.IsNullOrWhiteSpace(webhookUrl))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("\nEnter your Slack Incoming Webhook URL: ");
    Console.ResetColor();
    webhookUrl = Console.ReadLine()?.Trim() ?? string.Empty;
}

if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) || !uri.Host.Contains("slack.com"))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Must provide a valid Slack webhook URL (e.g. https://hooks.slack.com/services/...)");
    Console.ResetColor();
    return;
}

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger<SlackWebhookDeliveryService>();

var deliveryService = new SlackWebhookDeliveryService(new HttpClient(), logger);

var sampleSignal = IntentSignal.Create(
    companyName: "Acme Healthcare Systems",
    jobTitle: "VP of Enterprise Cloud Architecture",
    targetTechnologies: ["Azure", "Cloud Migration", ".NET 9", "SOC2"],
    sourceUrl: "https://careers.acmehealth.com/jobs/lead-cloud-902",
    dateDetected: DateTimeOffset.UtcNow
);

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine($"\n[DISPATCHING] Sending Live Test Lead to Slack...");
Console.WriteLine($" -> Company: {sampleSignal.CompanyName}");
Console.WriteLine($" -> Role:    {sampleSignal.JobTitle}");
Console.WriteLine($" -> Stack:   [{string.Join(", ", sampleSignal.TargetTechnologies)}]");
Console.WriteLine($" -> URL:     {sampleSignal.SourceUrl}\n");
Console.ResetColor();

var success = await deliveryService.DeliverSignalAsync(sampleSignal, webhookUrl, CancellationToken.None);

if (success)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("================================================================================");
    Console.WriteLine(" SUCCESS! High-Intent Lead successfully rendered in your Slack channel!");
    Console.WriteLine("================================================================================\n");
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Failed to deliver signal. Check that your Webhook URL is valid and channel permissions are intact.");
}
Console.ResetColor();
