using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Application.IntentSignals.Commands.SaveIntentSignal;
using StackTrigger.Domain.Entities;

namespace StackTrigger.Infrastructure.Scraping;

public sealed class EnterpriseAtsScraper : IIntentScraper
{
    private static readonly Regex SalaryRegex = new(
        @"\$[\d]{2,3}(?:,\d{3})*(?:\s*-\s*\$[\d]{2,3}(?:,\d{3})*|\s*(?:to|-)\s*\$[\d]{2,3}(?:,\d{3})*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly HttpClient _httpClient;
    private readonly AtsScraperOptions _options;
    private readonly ILogger<EnterpriseAtsScraper> _logger;
    private readonly AtsHtmlScraper _htmlScraperFallback;

    public EnterpriseAtsScraper(
        HttpClient httpClient,
        IOptions<AtsScraperOptions> options,
        ILogger<EnterpriseAtsScraper> logger,
        AtsHtmlScraper htmlScraperFallback)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? new AtsScraperOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _htmlScraperFallback = htmlScraperFallback ?? throw new ArgumentNullException(nameof(htmlScraperFallback));
    }

    public async Task<IEnumerable<SaveIntentSignalCommand>> ScrapeSignalsAsync(CancellationToken cancellationToken)
    {
        var discovered = new List<SaveIntentSignalCommand>();

        // 1. Greenhouse REST API Ingestion
        foreach (var company in _options.GreenhouseCompanies)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                var signals = await ScrapeGreenhouseCompanyAsync(company, cancellationToken);
                discovered.AddRange(signals);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scrape Greenhouse board for {Company}", company);
            }
        }

        // 2. Lever REST API Ingestion
        foreach (var company in _options.LeverCompanies)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                var signals = await ScrapeLeverCompanyAsync(company, cancellationToken);
                discovered.AddRange(signals);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scrape Lever board for {Company}", company);
            }
        }

        // 3. Fallback Curated HTML Boards
        try
        {
            var htmlSignals = await _htmlScraperFallback.ScrapeSignalsAsync(cancellationToken);
            discovered.AddRange(htmlSignals);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scrape fallback HTML boards");
        }

        return discovered;
    }

    private async Task<List<SaveIntentSignalCommand>> ScrapeGreenhouseCompanyAsync(string companySlug, CancellationToken ct)
    {
        var results = new List<SaveIntentSignalCommand>();
        var url = $"https://boards-api.greenhouse.io/v1/boards/{companySlug}/jobs?content=true";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return results;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("jobs", out var jobsArray)) return results;

        var formattedCompany = FormatSlugName(companySlug);

        foreach (var job in jobsArray.EnumerateArray())
        {
            var title = job.GetProperty("title").GetString() ?? string.Empty;
            var jobUrl = job.GetProperty("absolute_url").GetString() ?? string.Empty;
            var content = job.TryGetProperty("content", out var cp) ? cp.GetString() ?? string.Empty : string.Empty;

            var fullText = $"{title} {content}";
            var matches = EvaluateKeywords(fullText);

            if (matches.Count > 0 && !string.IsNullOrWhiteSpace(jobUrl))
            {
                var salary = ExtractSalary(fullText);
                var decisionMaker = IntentSignal.InferDecisionMakerTitle(title);
                var linkedInUrl = IntentSignal.GenerateLinkedInSearchUrl(formattedCompany, decisionMaker);
                var icebreaker = IntentSignal.GenerateOutreachIcebreaker(formattedCompany, title, matches);

                results.Add(new SaveIntentSignalCommand(
                    CompanyName: formattedCompany,
                    JobTitle: title,
                    TargetTechnologies: matches,
                    SourceUrl: jobUrl,
                    DateDetected: DateTimeOffset.UtcNow,
                    SalaryRange: salary,
                    DecisionMakerTitle: decisionMaker,
                    LinkedInSearchUrl: linkedInUrl,
                    OutreachIcebreaker: icebreaker
                ));
            }
        }

        return results;
    }

    private async Task<List<SaveIntentSignalCommand>> ScrapeLeverCompanyAsync(string companySlug, CancellationToken ct)
    {
        var results = new List<SaveIntentSignalCommand>();
        var url = $"https://api.lever.co/v0/postings/{companySlug}?mode=json";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return results;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var formattedCompany = FormatSlugName(companySlug);

        foreach (var posting in doc.RootElement.EnumerateArray())
        {
            var title = posting.GetProperty("text").GetString() ?? string.Empty;
            var jobUrl = posting.GetProperty("hostedUrl").GetString() ?? string.Empty;
            var description = posting.TryGetProperty("descriptionPlain", out var dp) ? dp.GetString() ?? string.Empty : string.Empty;

            var fullText = $"{title} {description}";
            var matches = EvaluateKeywords(fullText);

            if (matches.Count > 0 && !string.IsNullOrWhiteSpace(jobUrl))
            {
                var salary = ExtractSalary(fullText);
                var decisionMaker = IntentSignal.InferDecisionMakerTitle(title);
                var linkedInUrl = IntentSignal.GenerateLinkedInSearchUrl(formattedCompany, decisionMaker);
                var icebreaker = IntentSignal.GenerateOutreachIcebreaker(formattedCompany, title, matches);

                results.Add(new SaveIntentSignalCommand(
                    CompanyName: formattedCompany,
                    JobTitle: title,
                    TargetTechnologies: matches,
                    SourceUrl: jobUrl,
                    DateDetected: DateTimeOffset.UtcNow,
                    SalaryRange: salary,
                    DecisionMakerTitle: decisionMaker,
                    LinkedInSearchUrl: linkedInUrl,
                    OutreachIcebreaker: icebreaker
                ));
            }
        }

        return results;
    }

    private static string? ExtractSalary(string text)
    {
        var match = SalaryRegex.Match(text);
        return match.Success ? match.Value.Trim() : null;
    }

    private List<string> EvaluateKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        var matched = new List<string>();
        foreach (var keyword in _options.MonitoredKeywords)
        {
            var escaped = Regex.Escape(keyword);
            var pattern = $@"(?i)(?<=^|\s|[.,;:!?'""\(\)\[\]\/\\]){escaped}(?=$|\s|[.,;:!?'""\(\)\[\]\/\\])";
            if (Regex.IsMatch(text, pattern))
            {
                matched.Add(keyword);
            }
        }
        return matched;
    }

    private static string FormatSlugName(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return "Enterprise Target";
        return char.ToUpperInvariant(slug[0]) + slug[1..];
    }
}
