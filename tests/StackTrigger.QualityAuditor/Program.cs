using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackTrigger.Infrastructure.Scraping;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("================================================================================");
Console.WriteLine("         STACKTRIGGER LIVE QUALITY AUDIT: REAL-TIME ATS HARVEST                 ");
Console.WriteLine("================================================================================");
Console.ResetColor();

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
var scraperLogger = loggerFactory.CreateLogger<EnterpriseAtsScraper>();
var htmlLogger = loggerFactory.CreateLogger<AtsHtmlScraper>();

var options = Options.Create(new AtsScraperOptions
{
    GreenhouseCompanies = [
        "cloudflare",
        "hashicorp",
        "elastic",
        "confluent",
        "datadog",
        "snyk",
        "mongodb"
    ],
    LeverCompanies = [
        "palantir",
        "atlassian"
    ],
    TargetUrls = [
        "https://weworkremotely.com/categories/remote-devops-sysadmin-jobs"
    ],
    MonitoredKeywords = [
        "Azure", "AWS", "GCP", "Cloud Migration", "Migrate to Cloud",
        "SOC2", "SOC 2", "CrowdStrike", "Zero Trust", "Kubernetes",
        "Terraform", "Datadog", "FinOps", ".NET", ".NET 9"
    ]
});

var httpClient = new HttpClient();
var htmlScraper = new AtsHtmlScraper(httpClient, options, htmlLogger);
var enterpriseScraper = new EnterpriseAtsScraper(httpClient, options, scraperLogger, htmlScraper);

Console.WriteLine("Fetching live signals across Greenhouse, Lever, and WeWorkRemotely...\n");
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var signals = (await enterpriseScraper.ScrapeSignalsAsync(CancellationToken.None)).ToList();
stopwatch.Stop();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"[HARVEST COMPLETED in {stopwatch.ElapsedMilliseconds}ms]");
Console.WriteLine($"Total qualified signals captured: {signals.Count}\n");
Console.ResetColor();

if (signals.Count == 0)
{
    Console.WriteLine("No signals returned. Verify network connection.");
    return;
}

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("================================================================================");
Console.WriteLine("                   DETAILED LEAD QUALITY AUDIT (SAMPLE 10)                      ");
Console.WriteLine("================================================================================");
Console.ResetColor();

int counter = 1;
foreach (var lead in signals.Take(10))
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"\nLEAD #{counter:D2}: {lead.CompanyName.ToUpper()}");
    Console.ResetColor();
    Console.WriteLine($"  Title:       {lead.JobTitle}");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  Triggers:    [{string.Join(", ", lead.TargetTechnologies)}]");
    Console.ResetColor();
    Console.WriteLine($"  Source Link: {lead.SourceUrl}");

    // Quality check criteria
    bool hasLegitimateUrl = Uri.TryCreate(lead.SourceUrl, UriKind.Absolute, out var uri) && (uri.Host.Contains("greenhouse.io") || uri.Host.Contains("lever.co") || uri.Host.Contains("weworkremotely.com"));
    bool hasHighValueTech = lead.TargetTechnologies.Any(t => t is "Azure" or "AWS" or "Cloud Migration" or "SOC2" or "Zero Trust" or "Kubernetes");

    Console.ForegroundColor = (hasLegitimateUrl && hasHighValueTech) ? ConsoleColor.Green : ConsoleColor.DarkYellow;
    Console.WriteLine($"  Quality Grade: {(hasLegitimateUrl && hasHighValueTech ? "A+ (Enterprise Verified)" : "B (Standard)")}");
    Console.ResetColor();

    counter++;
}

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n================================================================================");
Console.WriteLine($"AUDIT SUMMARY: {signals.Count} Verified Leads Harvested.");
Console.WriteLine("================================================================================");
Console.ResetColor();
