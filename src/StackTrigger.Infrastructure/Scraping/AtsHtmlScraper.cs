using System.Text.RegularExpressions;
using System.Globalization;
using AngleSharp;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Application.IntentSignals.Commands.SaveIntentSignal;

namespace StackTrigger.Infrastructure.Scraping;

public sealed partial class AtsHtmlScraper : IIntentScraper
{
    public const string HttpClientName = "AtsScraperHttpClient";

    private readonly HttpClient _httpClient;
    private readonly AtsScraperOptions _options;
    private readonly ILogger<AtsHtmlScraper> _logger;
    private readonly IBrowsingContext _browsingContext;

    public AtsHtmlScraper(
        HttpClient httpClient,
        IOptions<AtsScraperOptions> options,
        ILogger<AtsHtmlScraper> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? new AtsScraperOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var configuration = Configuration.Default;
        _browsingContext = BrowsingContext.New(configuration);
    }

    public async Task<IEnumerable<SaveIntentSignalCommand>> ScrapeSignalsAsync(CancellationToken cancellationToken)
    {
        var discoveredCommands = new List<SaveIntentSignalCommand>();

        foreach (var targetUrl in _options.TargetUrls)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                _logger.LogInformation("Scraping ATS target: {Url}", targetUrl);

                using var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
                request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Failed to fetch {Url}. StatusCode: {StatusCode}",
                        targetUrl,
                        response.StatusCode);
                    continue;
                }

                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                var commands = await ParseHtmlContentAsync(html, targetUrl, cancellationToken);
                discoveredCommands.AddRange(commands);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("ATS scraping cancelled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping {Url}", targetUrl);
            }
        }

        return discoveredCommands;
    }

    public async Task<IReadOnlyList<SaveIntentSignalCommand>> ParseHtmlContentAsync(
        string html,
        string pageUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html, nameof(html));

        var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);

        // Support both aggregated boards and individual job posting pages
        var jobCards = document.QuerySelectorAll("[data-job-id], .job-posting, .posting, .job-item, article.job");

        var results = new List<SaveIntentSignalCommand>();

        if (jobCards.Length > 0)
        {
            foreach (var card in jobCards)
            {
                var command = ExtractFromJobElement(card, pageUrl);
                if (command != null)
                {
                    results.Add(command);
                }
            }
        }
        else
        {
            // Fallback for single job posting page
            var singleCommand = ExtractFromSingleJobDocument(document, pageUrl);
            if (singleCommand != null)
            {
                results.Add(singleCommand);
            }
        }

        return results;
    }

    private SaveIntentSignalCommand? ExtractFromJobElement(AngleSharp.Dom.IElement card, string fallbackUrl)
    {
        var title = card.QuerySelector(".job-title, [data-title], h2, h3, a.title")?.TextContent?.Trim();
        var company = card.QuerySelector(".company-name, .company, [data-company]")?.TextContent?.Trim();
        var link = card.QuerySelector("a[href]")?.GetAttribute("href");
        var description = card.QuerySelector(".job-description, .description, .summary, .job-details")?.TextContent?.Trim()
                          ?? card.TextContent;

        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var sourceUrl = ResolveUrl(link, fallbackUrl);
        var companyName = !string.IsNullOrWhiteSpace(company) ? company : ExtractCompanyFromHost(sourceUrl);
        var sourceDate = ExtractSourceDate(card);

        if (!IsRecentSourceDate(sourceDate))
        {
            _logger.LogDebug("Skipping HTML posting without a recent source date: {SourceUrl}", sourceUrl);
            return null;
        }

        var matchingKeywords = EvaluateKeywords($"{title} {description}");
        var relevanceScore = ScoreRole(title, matchingKeywords);
        if (relevanceScore == 0)
        {
            return null;
        }

        return new SaveIntentSignalCommand(
            CompanyName: companyName,
            JobTitle: title,
            TargetTechnologies: matchingKeywords,
            SourceUrl: sourceUrl,
            DateDetected: DateTimeOffset.UtcNow,
            SourceDate: sourceDate,
            RelevanceScore: relevanceScore
        );
    }

    private SaveIntentSignalCommand? ExtractFromSingleJobDocument(AngleSharp.Dom.IDocument document, string sourceUrl)
    {
        var title = document.QuerySelector("h1.job-title, h1, [data-testid='job-title']")?.TextContent?.Trim();
        var company = document.QuerySelector(".company-name, [data-testid='company-name'], .employer-name")?.TextContent?.Trim();
        var description = document.QuerySelector(".job-description, #job-description, main, body")?.TextContent?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var companyName = !string.IsNullOrWhiteSpace(company) ? company : ExtractCompanyFromHost(sourceUrl);
        var sourceDate = ExtractSourceDate(document);

        if (!IsRecentSourceDate(sourceDate))
        {
            _logger.LogDebug("Skipping HTML posting without a recent source date: {SourceUrl}", sourceUrl);
            return null;
        }

        var matchingKeywords = EvaluateKeywords($"{title} {description}");
        var relevanceScore = ScoreRole(title, matchingKeywords);
        if (relevanceScore == 0)
        {
            return null;
        }

        return new SaveIntentSignalCommand(
            CompanyName: companyName,
            JobTitle: title,
            TargetTechnologies: matchingKeywords,
            SourceUrl: sourceUrl,
            DateDetected: DateTimeOffset.UtcNow,
            SourceDate: sourceDate,
            RelevanceScore: relevanceScore
        );
    }

    private static DateTimeOffset? ExtractSourceDate(AngleSharp.Dom.IParentNode node)
    {
        var dateElement = node.QuerySelector(
            "time[datetime], [data-posted-at], [data-date-posted], [data-created-at], [itemprop='datePosted'], meta[name='date']");
        if (dateElement is null) return null;

        var value = dateElement.GetAttribute("datetime")
                    ?? dateElement.GetAttribute("data-posted-at")
                    ?? dateElement.GetAttribute("data-date-posted")
                    ?? dateElement.GetAttribute("data-created-at")
                    ?? dateElement.GetAttribute("content")
                    ?? dateElement.TextContent;

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;
    }

    private bool IsRecentSourceDate(DateTimeOffset? sourceDate)
    {
        if (sourceDate is null) return false;

        var now = DateTimeOffset.UtcNow;
        var maxAgeDays = Math.Max(1, _options.MaxPostingAgeDays);
        return sourceDate.Value <= now.AddDays(1) && sourceDate.Value >= now.AddDays(-maxAgeDays);
    }

    private static int ScoreRole(string title, IReadOnlyCollection<string> matchedKeywords)
    {
        if (matchedKeywords.Count == 0) return 0;

        string[] relevantRoleTerms =
        [
            "cloud", "security", "cyber", "platform", "infrastructure", "devops", "sre",
            "architect", "engineer", "compliance", "technology", "systems", "network",
            "site reliability", "information technology", "it ", "it/"
        ];

        if (!relevantRoleTerms.Any(term => title.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            return 0;
        }

        return 2 + matchedKeywords.Count +
            matchedKeywords.Count(keyword => title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) * 3;
    }

    private List<string> EvaluateKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var matched = new List<string>();

        foreach (var keyword in _options.MonitoredKeywords)
        {
            // Exact word boundary match, case-insensitive, handling symbols like .NET and C#
            var escapedKeyword = Regex.Escape(keyword);
            var pattern = $@"(?i)(?<=^|\s|[.,;:!?'""\(\)\[\]\/\\]){escapedKeyword}(?=$|\s|[.,;:!?'""\(\)\[\]\/\\])";

            if (Regex.IsMatch(text, pattern))
            {
                matched.Add(keyword);
            }
        }

        return matched;
    }

    private static string ResolveUrl(string? relativeOrAbsoluteUrl, string fallbackUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUrl))
        {
            return fallbackUrl;
        }

        if (Uri.TryCreate(relativeOrAbsoluteUrl, UriKind.Absolute, out var absUri))
        {
            return absUri.ToString();
        }

        if (Uri.TryCreate(new Uri(fallbackUrl), relativeOrAbsoluteUrl, out var combinedUri))
        {
            return combinedUri.ToString();
        }

        return fallbackUrl;
    }

    private static string ExtractCompanyFromHost(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var hostParts = uri.Host.Split('.');
            if (hostParts.Length >= 2)
            {
                return char.ToUpperInvariant(hostParts[0][0]) + hostParts[0][1..];
            }
        }

        return "Unknown Company";
    }
}

