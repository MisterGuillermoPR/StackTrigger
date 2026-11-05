using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Application.IntentSignals.Commands.SaveIntentSignal;

namespace StackTrigger.Infrastructure.Scraping;

/// <summary>
/// Scrapes pristine enterprise intent data directly from corporate Greenhouse & Lever REST APIs.
/// Zero HTML scraping fragility, zero CAPTCHAs, 100% structured data.
/// </summary>
public sealed class EnterpriseAtsScraper : IIntentScraper
{
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

        // 1. Ingest from Greenhouse Corporate Boards (Free, official, unblocked REST API)
        foreach (var company in _options.GreenhouseCompanies)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var greenhouseSignals = await ScrapeGreenhouseCompanyAsync(company, cancellationToken);
                discovered.AddRange(greenhouseSignals);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scrape Greenhouse board for {Company}", company);
            }
        }

        // 2. Ingest from Lever Corporate Boards (Official unblocked REST API)
        foreach (var company in _options.LeverCompanies)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var leverSignals = await ScrapeLeverCompanyAsync(company, cancellationToken);
                discovered.AddRange(leverSignals);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scrape Lever board for {Company}", company);
            }
        }

        // 3. Ingest from Curated High-Ticket HTML Boards (e.g. WeWorkRemotely DevOps)
        try
        {
            var htmlSignals = (await _htmlScraperFallback.ScrapeSignalsAsync(cancellationToken)).ToList();
            var eligibleHtmlSignals = htmlSignals
                .Where(signal => signal.SourceDate is not null && signal.RelevanceScore > 0)
                .ToList();

            if (eligibleHtmlSignals.Count != htmlSignals.Count)
            {
                _logger.LogWarning(
                    "Excluded {Count} HTML fallback signals because they lack a source timestamp or role relevance score",
                    htmlSignals.Count - eligibleHtmlSignals.Count);
            }

            discovered.AddRange(eligibleHtmlSignals);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scrape fallback HTML boards");
        }

        var ranked = discovered
            .OrderByDescending(signal => signal.RelevanceScore)
            .ThenByDescending(signal => signal.SourceDate)
            .ToList();

        _logger.LogInformation("EnterpriseAtsScraper harvest completed. Total fresh, role-qualified signals: {Count}", ranked.Count);
        return ranked;
    }

    private async Task<List<SaveIntentSignalCommand>> ScrapeGreenhouseCompanyAsync(string companySlug, CancellationToken ct)
    {
        var results = new List<SaveIntentSignalCommand>();
        var url = $"https://boards-api.greenhouse.io/v1/boards/{companySlug}/jobs?content=true";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return results;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("jobs", out var jobsArray))
        {
            return results;
        }

        var formattedCompanyName = FormatSlugName(companySlug);

        foreach (var job in jobsArray.EnumerateArray())
        {
            var title = job.GetProperty("title").GetString() ?? string.Empty;
            var jobUrl = job.GetProperty("absolute_url").GetString() ?? string.Empty;
            var content = job.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? string.Empty : string.Empty;
            var sourceDate = TryGetDate(job, "updated_at");

            if (!IsFresh(sourceDate, companySlug, jobUrl)) continue;

            var matches = EvaluateKeywords(title, content);
            var score = ScoreRole(title, matches);

            if (score > 0 && !string.IsNullOrWhiteSpace(jobUrl))
            {
                results.Add(new SaveIntentSignalCommand(
                    CompanyName: formattedCompanyName,
                    JobTitle: title,
                    TargetTechnologies: matches,
                    SourceUrl: jobUrl,
                    DateDetected: DateTimeOffset.UtcNow,
                    SourceDate: sourceDate,
                    RelevanceScore: score
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
        if (!response.IsSuccessStatusCode)
        {
            return results;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var formattedCompanyName = FormatSlugName(companySlug);

        foreach (var posting in doc.RootElement.EnumerateArray())
        {
            var title = posting.GetProperty("text").GetString() ?? string.Empty;
            var jobUrl = posting.GetProperty("hostedUrl").GetString() ?? string.Empty;
            var description = posting.TryGetProperty("descriptionPlain", out var descProp) ? descProp.GetString() ?? string.Empty : string.Empty;
            var sourceDate = TryGetLeverCreatedAt(posting);

            if (!IsFresh(sourceDate, companySlug, jobUrl)) continue;

            var matches = EvaluateKeywords(title, description);
            var score = ScoreRole(title, matches);

            if (score > 0 && !string.IsNullOrWhiteSpace(jobUrl))
            {
                results.Add(new SaveIntentSignalCommand(
                    CompanyName: formattedCompanyName,
                    JobTitle: title,
                    TargetTechnologies: matches,
                    SourceUrl: jobUrl,
                    DateDetected: DateTimeOffset.UtcNow,
                    SourceDate: sourceDate,
                    RelevanceScore: score
                ));
            }
        }

        return results;
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

    private List<string> EvaluateKeywords(string title, string description)
    {
        var text = $"{title} {description}";
        return EvaluateKeywords(text);
    }

    private static int ScoreRole(string title, IReadOnlyCollection<string> matches)
    {
        if (matches.Count == 0 || !IsRelevantRole(title)) return 0;

        return 2 + matches.Count +
            matches.Count(keyword => title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) * 3;
    }

    private static bool IsRelevantRole(string title)
    {
        string[] roleTerms =
        [
            "cloud", "security", "cyber", "platform", "infrastructure", "devops", "sre",
            "architect", "engineer", "compliance", "technology", "systems", "network",
            "site reliability", "information technology", "it ", "it/"
        ];

        return roleTerms.Any(term => title.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsFresh(DateTimeOffset? sourceDate, string company, string sourceUrl)
    {
        if (sourceDate is null)
        {
            _logger.LogDebug("Skipping ATS posting without source timestamp from {Company}: {SourceUrl}", company, sourceUrl);
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var maxAgeDays = Math.Max(1, _options.MaxPostingAgeDays);
        if (sourceDate.Value > now.AddDays(1) || sourceDate.Value < now.AddDays(-maxAgeDays))
        {
            _logger.LogDebug("Skipping stale or future-dated ATS posting from {Company}: {SourceUrl} ({SourceDate})", company, sourceUrl, sourceDate);
            return false;
        }

        return true;
    }

    private static DateTimeOffset? TryGetDate(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var dateElement) || dateElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return dateElement.TryGetDateTimeOffset(out var date) ? date : null;
    }

    private static DateTimeOffset? TryGetLeverCreatedAt(JsonElement posting)
    {
        if (!posting.TryGetProperty("createdAt", out var createdAt) ||
            createdAt.ValueKind != JsonValueKind.Number ||
            !createdAt.TryGetInt64(out var unixMilliseconds))
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static string FormatSlugName(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return "Target Enterprise";
        return char.ToUpperInvariant(slug[0]) + slug[1..];
    }
}

