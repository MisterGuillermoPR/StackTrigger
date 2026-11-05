using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackTrigger.Domain.Entities;
using StackTrigger.Infrastructure.Scraping;
using StackTrigger.Infrastructure.Webhooks;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("================================================================================");
Console.WriteLine("        STACKTRIGGER ENTERPRISE ATS HARVESTER -> LIVE SLACK DISPATCH           ");
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
    Console.WriteLine("Error: Must provide a valid Slack webhook URL (https://hooks.slack.com/services/...)");
    Console.ResetColor();
    return;
}

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
var scraperLogger = loggerFactory.CreateLogger<EnterpriseAtsScraper>();
var htmlLogger = loggerFactory.CreateLogger<AtsHtmlScraper>();
var webhookLogger = loggerFactory.CreateLogger<SlackWebhookDeliveryService>();

var options = Options.Create(new AtsScraperOptions
{
    // Fast test targets: 3 top tech companies currently hiring heavily
    GreenhouseCompanies = ["cloudflare", "elastic", "mongodb"],
    LeverCompanies = ["palantir"],
    TargetUrls = [],
    MonitoredKeywords = ["Azure", "AWS", "GCP", "Kubernetes", "SOC2", "Cloud Migration", "Zero Trust", "Datadog", "Terraform"]
});

var httpClient = new HttpClient();
var htmlScraper = new AtsHtmlScraper(httpClient, options, htmlLogger);
var enterpriseScraper = new EnterpriseAtsScraper(httpClient, options, scraperLogger, htmlScraper);
var webhookService = new SlackWebhookDeliveryService(httpClient, webhookLogger);

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\n[1/2] Harvesting Live Enterprise ATS APIs (Cloudflare, Elastic, MongoDB, Palantir)...");
Console.ResetColor();

var signals = (await enterpriseScraper.ScrapeSignalsAsync(CancellationToken.None)).ToList();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n[+] Successfully harvested {signals.Count} qualified enterprise signals matching intent keywords!");
Console.ResetColor();

if (signals.Count == 0)
{
    Console.WriteLine("No signals matched criteria right now. Check network or keywords.");
    return;
}

// Show the top 3 live leads
Console.WriteLine("\nSample Discovered Live Leads:");
foreach (var lead in signals.Take(3))
{
    Console.WriteLine($" - [{lead.CompanyName}] {lead.JobTitle}");
    Console.WriteLine($"   Tags: {string.Join(", ", lead.TargetTechnologies)}");
    Console.WriteLine($"   Role relevance: {lead.RelevanceScore} | ATS source date: {lead.SourceDate:yyyy-MM-dd}");
    Console.WriteLine($"   URL:  {lead.SourceUrl}\n");
}

Console.ForegroundColor = ConsoleColor.Yellow;
Console.Write("Dispatch the top lead directly to your Slack channel? (y/n): ");
Console.ResetColor();
var confirm = Console.ReadLine()?.Trim().ToLowerInvariant();

if (confirm == "y" || confirm == "yes")
{
    var topLead = signals.First();
    var signalEntity = IntentSignal.Create(
        topLead.CompanyName,
        topLead.JobTitle,
        topLead.TargetTechnologies,
        topLead.SourceUrl,
        topLead.DateDetected
    );

    Console.WriteLine($"\nDispatching '{topLead.CompanyName} - {topLead.JobTitle}' to Slack...");
    var success = await webhookService.DeliverSignalAsync(signalEntity, webhookUrl, CancellationToken.None);

    if (success)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n================================================================================");
        Console.WriteLine(" SUCCESS! REAL ENTERPRISE INTENT LEAD PUSHED DIRECTLY TO YOUR SLACK CHANNEL!   ");
        Console.WriteLine("================================================================================\n");
    }
}
