using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackTrigger.Domain.Entities;
using StackTrigger.Infrastructure.Enrichment;
using StackTrigger.Infrastructure.Scraping;
using StackTrigger.Infrastructure.Webhooks;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("================================================================================");
Console.WriteLine("      STACKTRIGGER $0 DECISION-MAKER ENRICHMENT -> LIVE SLACK DEMO              ");
Console.WriteLine("================================================================================");
Console.ResetColor();

var webhookUrl = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("SLACK_WEBHOOK_URL") ?? string.Empty;

if (string.IsNullOrWhiteSpace(webhookUrl))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("\nEnter your Slack Incoming Webhook URL: ");
    Console.ResetColor();
    webhookUrl = Console.ReadLine()?.Trim() ?? string.Empty;
}

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
var discoveryLogger = loggerFactory.CreateLogger<FreeExecutiveDiscoveryService>();
var webhookLogger = loggerFactory.CreateLogger<SlackWebhookDeliveryService>();

var httpClient = new HttpClient();
var discoveryService = new FreeExecutiveDiscoveryService(httpClient, discoveryLogger);
var webhookService = new SlackWebhookDeliveryService(httpClient, webhookLogger);

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\n[1/2] Discovering real executive decision-makers for $0 (Zero API keys)...");
Console.ResetColor();

// Test with real high-profile enterprise targets
var targetCompany = "Datadog";
var targetRole = "Lead Cloud Security & Zero Trust Architect";

var discoveredLeader = await discoveryService.FindDecisionMakerAsync(targetCompany, targetRole, CancellationToken.None);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n[+] Discovered Real Decision-Maker for {targetCompany}:");
Console.WriteLine($" -> Name:    {discoveredLeader?.FullName}");
Console.WriteLine($" -> Title:   {discoveredLeader?.JobTitle}");
Console.WriteLine($" -> Profile: {discoveredLeader?.DirectProfileUrl}\n");
Console.ResetColor();

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("[2/2] Formatting enriched Slack Block Kit card and dispatching...");
Console.ResetColor();

var signal = IntentSignal.Create(
    companyName: targetCompany,
    jobTitle: targetRole,
    targetTechnologies: ["Azure", "AWS", "Zero Trust", "Kubernetes"],
    sourceUrl: "https://careers.datadoghq.com/detail/7194969/?gh_jid=7194969",
    dateDetected: DateTimeOffset.UtcNow,
    salaryRange: "$195,000 - $245,000 USD",
    decisionMakerTitle: $"{discoveredLeader?.FullName} ({discoveredLeader?.JobTitle})",
    linkedInSearchUrl: discoveredLeader?.DirectProfileUrl,
    outreachIcebreaker: $"\"Hi {discoveredLeader?.FullName?.Split(' ')[0] ?? "there"}, saw {targetCompany} is expanding its Zero Trust and Cloud Security team. Most teams hit a capacity wall during this rollout—are you handling architecture internally or using specialized advisory?\""
);

var success = await webhookService.DeliverSignalAsync(signal, webhookUrl, CancellationToken.None);

if (success)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("================================================================================");
    Console.WriteLine(" SUCCESS! $0 ENRICHED DECISION-MAKER LEAD DISPATCHED TO YOUR SLACK CHANNEL!   ");
    Console.WriteLine("================================================================================\n");
}
