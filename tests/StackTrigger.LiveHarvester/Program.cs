using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackTrigger.Infrastructure.Scraping;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("==========================================================================");
Console.WriteLine("           STACKTRIGGER AWS + AZURE HIRING SIGNAL REVIEW                 ");
Console.WriteLine("                 READ-ONLY HARVEST — NO SLACK DISPATCH                    ");
Console.WriteLine("==========================================================================");
Console.ResetColor();

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));

var scraperLogger = loggerFactory.CreateLogger<EnterpriseAtsScraper>();
var htmlLogger = loggerFactory.CreateLogger<AtsHtmlScraper>();
var options = Options.Create(new AtsScraperOptions
{
    // This is a small technical-employer sample, not a representative customer market sample.
    GreenhouseCompanies = ["cloudflare", "elastic", "mongodb"],
    LeverCompanies = ["palantir"],
    TargetUrls = [],
    MonitoredKeywords = ["AWS", "Azure", "Cloud Migration", "Migrate to Cloud", "Terraform", "Kubernetes"],
    MaxPostingAgeDays = 30
});

using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
var htmlScraper = new AtsHtmlScraper(httpClient, options, htmlLogger);
var enterpriseScraper = new EnterpriseAtsScraper(httpClient, options, scraperLogger, htmlScraper);
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));

Console.WriteLine("Scanning 3 Greenhouse boards and 1 Lever board; maximum source age: 30 days.");
var harvested = (await enterpriseScraper.ScrapeSignalsAsync(timeout.Token)).ToList();

var signals = harvested
    .Where(signal => signal.TargetTechnologies.Any(tag =>
        tag.Equals("AWS", StringComparison.OrdinalIgnoreCase) ||
        tag.Equals("Azure", StringComparison.OrdinalIgnoreCase)))
    .ToList();

var awsCount = signals.Count(signal => signal.TargetTechnologies.Contains("AWS", StringComparer.OrdinalIgnoreCase));
var azureCount = signals.Count(signal => signal.TargetTechnologies.Contains("Azure", StringComparer.OrdinalIgnoreCase));
Console.WriteLine($"Fresh, role-relevant AWS/Azure signals: {signals.Count} (AWS: {awsCount}, Azure: {azureCount}).");
Console.WriteLine("AWS/Azure counts can overlap when a posting mentions both.");

var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "artifacts");
Directory.CreateDirectory(outputDirectory);
var outputPath = Path.Combine(outputDirectory, "pilot-signals-review.csv");
var csv = new StringBuilder();
csv.AppendLine("Company,Role,Platform,Other matched terms,ATS source timestamp,Role relevance score,Source URL");

foreach (var signal in signals)
{
    var platforms = signal.TargetTechnologies
        .Where(tag => tag.Equals("AWS", StringComparison.OrdinalIgnoreCase) || tag.Equals("Azure", StringComparison.OrdinalIgnoreCase));
    var otherTerms = signal.TargetTechnologies
        .Where(tag => !tag.Equals("AWS", StringComparison.OrdinalIgnoreCase) && !tag.Equals("Azure", StringComparison.OrdinalIgnoreCase));

    csv.AppendLine(string.Join(',', new[]
    {
        signal.CompanyName,
        signal.JobTitle,
        string.Join("; ", platforms),
        string.Join("; ", otherTerms),
        signal.SourceDate?.ToString("O") ?? string.Empty,
        signal.RelevanceScore.ToString(),
        signal.SourceUrl
    }.Select(EscapeCsv)));
}

await File.WriteAllTextAsync(outputPath, csv.ToString(), Encoding.UTF8, timeout.Token);
Console.WriteLine($"Review file: {outputPath}");

if (signals.Count == 0)
{
    Console.WriteLine("No qualifying signals found. The CSV contains headers only; do not infer demand from this run.");
    return;
}

Console.WriteLine("\nHighest role-relevance examples (review each source before using it as a prospecting lead):");
foreach (var signal in signals.Take(10))
{
    var platforms = signal.TargetTechnologies
        .Where(tag => tag.Equals("AWS", StringComparison.OrdinalIgnoreCase) || tag.Equals("Azure", StringComparison.OrdinalIgnoreCase));
    Console.WriteLine($"- [{string.Join("/", platforms)}] {signal.CompanyName} — {signal.JobTitle}");
    Console.WriteLine($"  ATS source timestamp: {signal.SourceDate:yyyy-MM-dd} | Role relevance: {signal.RelevanceScore}");
    Console.WriteLine($"  {signal.SourceUrl}");
}

Console.WriteLine("\nThese are hiring signals, not evidence that a company wants to buy outside services.");

static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
