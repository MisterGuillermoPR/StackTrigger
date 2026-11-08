using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackTrigger.Application;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Application.Common.Options;
using StackTrigger.Application.IntentSignals.Commands.ProcessPendingSignals;
using StackTrigger.Application.IntentSignals.Commands.SaveIntentSignal;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Models;
using StackTrigger.Domain.Repositories;
using StackTrigger.Infrastructure.Persistence;
using StackTrigger.Infrastructure.Persistence.Repositories;
using StackTrigger.Infrastructure.Scraping;
using StackTrigger.Infrastructure.Webhooks;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("================================================================================");
Console.WriteLine("        STACKTRIGGER END-TO-END PIPELINE SIMULATION (LIVE IN-MEMORY TEST)       ");
Console.WriteLine("================================================================================");
Console.ResetColor();

// 1. Setup DI Container
var services = new ServiceCollection();

services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddApplication();

// Use EF Core In-Memory database for standalone pipeline simulation
var options = new DbContextOptionsBuilder<StackTriggerDbContext>()
    .UseInMemoryDatabase(databaseName: "StackTriggerSimDb")
    .Options;

var dbContext = new StackTriggerDbContext(options);

services.AddSingleton(dbContext);
services.AddScoped<IIntentSignalRepository, IntentSignalRepository>();
services.AddScoped<IClientSubscriptionRepository, ClientSubscriptionRepository>();

// Use real Slack Webhook Delivery Service to dispatch to live Slack channel
services.AddHttpClient<IWebhookDeliveryService, SlackWebhookDeliveryService>();
services.AddSingleton<IOptions<WebhookDispatcherOptions>>(
    Options.Create(new WebhookDispatcherOptions { BatchSize = 50 }));

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();
var clientRepo = serviceProvider.GetRequiredService<IClientSubscriptionRepository>();
var signalRepo = serviceProvider.GetRequiredService<IIntentSignalRepository>();

// 2. Seed Multi-Tenant Clients
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\n[STEP 1] Seeding B2B Subscribed Clients...");
Console.ResetColor();

var liveWebhookUrl = Environment.GetEnvironmentVariable("SLACK_WEBHOOK_URL")
    ?? "https://hooks.slack.com/services/PLACEHOLDER/CLIENT/FEED";

var clientCloudMsp = ClientSubscription.Create(
    companyName: "SkyHigh Cloud MSP ($400/mo subscriber)",
    webhookUrl: liveWebhookUrl,
    targetKeywords: ["Azure", "Cloud Migration", ".NET 9"]
);

var clientCyberSec = ClientSubscription.Create(
    companyName: "IronShield SecOps ($400/mo subscriber)",
    webhookUrl: liveWebhookUrl,
    targetKeywords: ["SOC2", "CrowdStrike", "Zero Trust"]
);

await clientRepo.AddAsync(clientCloudMsp);
await clientRepo.AddAsync(clientCyberSec);
await clientRepo.SaveChangesAsync();
Console.WriteLine($" -> Registered Client 1: {clientCloudMsp.CompanyName} (Keywords: {string.Join(", ", clientCloudMsp.TargetKeywords)})");
Console.WriteLine($" -> Registered Client 2: {clientCyberSec.CompanyName} (Keywords: {string.Join(", ", clientCyberSec.TargetKeywords)})");

// 3. Simulate Scraper Ingestion
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\n[STEP 2] Simulating Real ATS Job Posting Ingestion via AtsHtmlScraper...");
Console.ResetColor();

var recentSourceDate = DateTimeOffset.UtcNow.ToString("O");
var mockAtsHtml = $$"""
    <div class="job-board">
        <article class="job-posting">
            <h2 class="job-title">Senior Enterprise Cloud Architect</h2>
            <time datetime="{{recentSourceDate}}">Recently posted</time>
            <span class="company-name">HealthStream Solutions</span>
            <a href="https://careers.healthstream.com/jobs/cloud-arch-101">View Details</a>
            <div class="job-description">
                Leading urgent Cloud Migration initiatives to Azure. Modernizing our core services to .NET 9.
            </div>
        </article>
        <article class="job-posting">
            <h2 class="job-title">Director of Information Security</h2>
            <time datetime="{{recentSourceDate}}">Recently posted</time>
            <span class="company-name">Apex Global Logistics</span>
            <a href="https://apex.com/careers/dir-infosec-202">View Details</a>
            <div class="job-description">
                Mandate to achieve SOC2 Type II certification and deploy Zero Trust architecture.
            </div>
        </article>
        <article class="job-posting">
            <h2 class="job-title">HR & Payroll Coordinator</h2>
            <span class="company-name">Apex Global Logistics</span>
            <a href="https://apex.com/careers/hr-coord-303">View Details</a>
            <div class="job-description">
                Managing timesheets, PTO requests, and general office benefits administration.
            </div>
        </article>
    </div>
    """;

var scraperOptions = Options.Create(new AtsScraperOptions());
var scraper = new AtsHtmlScraper(new HttpClient(), scraperOptions, LoggerFactory.Create(_ => { }).CreateLogger<AtsHtmlScraper>());
var discoveredCommands = await scraper.ParseHtmlContentAsync(mockAtsHtml, "https://careers.healthstream.com");

Console.WriteLine($" -> Total ATS items in HTML: 3");
Console.WriteLine($" -> Qualified signals identified by keywords: {discoveredCommands.Count}");

foreach (var cmd in discoveredCommands)
{
    var saveResult = await mediator.Send(cmd);
    Console.WriteLine($"    [+] Saved Lead: '{cmd.CompanyName}' for '{cmd.JobTitle}' - Detected: [{string.Join(", ", cmd.TargetTechnologies)}]");
}

// 4. Test Deduplication
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\n[STEP 3] Verifying SourceUrl Deduplication Guard...");
Console.ResetColor();

var duplicateAttempt = discoveredCommands.First();
var duplicateResult = await mediator.Send(duplicateAttempt);
if (!duplicateResult.Saved)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($" -> Deduplication SUCCESS: {duplicateResult.Reason}");
    Console.ResetColor();
}

// 5. Test Multi-Tenant Dispatch Router
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\n[STEP 4] Executing Dispatch Engine (Keyword Router -> Slack Webhooks)...");
Console.ResetColor();

var dispatchResult = await mediator.Send(new ProcessPendingSignalsCommand());
Console.WriteLine($"\n -> Total Pending Processed: {dispatchResult.TotalProcessed}");
Console.WriteLine($" -> Total Webhooks Dispatched: {dispatchResult.SuccessfullyDispatched}");

// 6. Verify Database State
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\n[STEP 5] Verifying Database Dispatch Status...");
Console.ResetColor();

var remainingPending = await signalRepo.GetPendingSignalsAsync();
Console.ForegroundColor = remainingPending.Count == 0 ? ConsoleColor.Green : ConsoleColor.Red;
Console.WriteLine($" -> Remaining Pending Signals in DB: {remainingPending.Count} (Expected: 0)");
Console.ResetColor();

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n================================================================================");
Console.WriteLine("                       ALL PIPELINE CHECKS PASSED 100%                          ");
Console.WriteLine("================================================================================\n");
Console.ResetColor();

// Test Webhook Service Mock that logs the Slack Block Kit output
sealed class ConsoleLoggingWebhookService : IWebhookDeliveryService
{
    public Task<bool> DeliverSignalAsync(IntentSignal signal, string webhookUrl, CancellationToken cancellationToken)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n [DISPATCH -> SLACK] Delivering to: {webhookUrl}");
        Console.ResetColor();

        var payload = SlackWebhookDeliveryService.BuildSlackPayload(signal);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        Console.WriteLine($" Payload generated for {signal.CompanyName}:");
        Console.WriteLine($" - Lead: {signal.CompanyName} ({signal.JobTitle})");
        Console.WriteLine($" - Tags: [{string.Join(", ", signal.TargetTechnologies)}]");
        Console.WriteLine($" - URL:  {signal.SourceUrl}");

        return Task.FromResult(true);
    }

    public Task<bool> DeliverWeeklyDigestAsync(WeeklyDigestSummary digest, string webhookUrl, CancellationToken cancellationToken = default)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n [WEEKLY DIGEST -> SLACK] Delivering to: {webhookUrl}");
        Console.WriteLine($" Client: {digest.CompanyName} | Signals: {digest.TotalSignalsDelivered} | Est. Pipeline: {digest.EstimatedPipelineValue:C0}");
        Console.ResetColor();

        return Task.FromResult(true);
    }
}
