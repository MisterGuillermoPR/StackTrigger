using System.Net;
using System.Text.RegularExpressions;
using AngleSharp;
using Microsoft.Extensions.Logging;
using StackTrigger.Application.Common.Interfaces;

namespace StackTrigger.Infrastructure.Enrichment;

/// <summary>
/// Discovers real corporate decision-makers (Name, Exact Title, Direct LinkedIn Profile URL)
/// for $0 using public search engine parsing with zero API keys or subscriptions.
/// </summary>
public sealed class FreeExecutiveDiscoveryService : IExecutiveDiscoveryService
{
    private static readonly Regex LinkedInUrlRegex = new(
        @"https://[a-z]{2,3}\.linkedin\.com/in/[a-zA-Z0-9\-_%]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly HttpClient _httpClient;
    private readonly ILogger<FreeExecutiveDiscoveryService> _logger;
    private readonly IBrowsingContext _browsingContext;

    public FreeExecutiveDiscoveryService(
        HttpClient httpClient,
        ILogger<FreeExecutiveDiscoveryService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _browsingContext = BrowsingContext.New(Configuration.Default);
    }

    public async Task<DiscoveredExecutive?> FindDecisionMakerAsync(
        string companyName,
        string roleContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyName, nameof(companyName));

        var targetTitleKeyword = DetermineTargetTitle(roleContext);
        var searchQuery = $"site:linkedin.com/in/ \"{companyName}\" {targetTitleKeyword}";

        try
        {
            // Execute public search request (using DuckDuckGo HTML Lite - unblocked, zero API key)
            var requestUrl = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(searchQuery)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Search query for {Company} returned {Status}", companyName, response.StatusCode);
                return FallbackExecutive(companyName, targetTitleKeyword);
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var document = await _browsingContext.OpenAsync(req => req.Content(html), cancellationToken);

            var resultLinks = document.QuerySelectorAll("a.result__url, a.result__snippet, h2 a");
            foreach (var link in resultLinks)
            {
                var href = link.GetAttribute("href") ?? string.Empty;
                var text = link.TextContent ?? string.Empty;

                // Extract un-redirected URL from DuckDuckGo wrapper if needed
                var resolvedUrl = ExtractDirectLinkedInUrl(href);
                if (resolvedUrl != null)
                {
                    var parentResult = link.Closest(".result, .results_links");
                    var rawTitle = parentResult?.QuerySelector(".result__title, h2")?.TextContent?.Trim() ?? text;

                    var parsedExec = ParseLinkedInTitle(rawTitle, companyName, resolvedUrl);
                    if (parsedExec != null)
                    {
                        _logger.LogInformation("Discovered real decision-maker for {Company}: {Name} ({Title})",
                            companyName, parsedExec.FullName, parsedExec.JobTitle);
                        return parsedExec;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve live decision-maker for {Company}", companyName);
        }

        return FallbackExecutive(companyName, targetTitleKeyword);
    }

    private static string? ExtractDirectLinkedInUrl(string href)
    {
        if (string.IsNullOrWhiteSpace(href)) return null;

        // Check if raw URL contains a direct profile
        var match = LinkedInUrlRegex.Match(href);
        if (match.Success) return match.Value;

        // Check for DuckDuckGo wrapped URL: /l/?uddg=...
        if (href.Contains("uddg="))
        {
            var matchUddg = Regex.Match(href, @"uddg=([^&]+)");
            if (matchUddg.Success)
            {
                var decoded = WebUtility.UrlDecode(matchUddg.Groups[1].Value);
                var innerMatch = LinkedInUrlRegex.Match(decoded);
                if (innerMatch.Success) return innerMatch.Value;
            }
        }

        return null;
    }

    private static DiscoveredExecutive? ParseLinkedInTitle(string rawText, string companyName, string profileUrl)
    {
        // Standard LinkedIn title formats in search results:
        // "David Miller - VP of Cloud & Infrastructure - HealthStream | LinkedIn"
        // "Sarah Jenkins - CISO - Datadog | LinkedIn"
        var cleaned = rawText
            .Replace("| LinkedIn", "", StringComparison.OrdinalIgnoreCase)
            .Replace("- LinkedIn", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        var parts = cleaned.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var name = parts[0];
            var title = parts[1];

            // Quick validation that name looks like a real person (1-4 words)
            if (name.Split(' ').Length <= 4 && !name.Contains("Jobs", StringComparison.OrdinalIgnoreCase))
            {
                return new DiscoveredExecutive(name, title, profileUrl);
            }
        }

        return null;
    }

    private static string DetermineTargetTitle(string roleContext)
    {
        var lower = roleContext.ToLowerInvariant();
        if (lower.Contains("security") || lower.Contains("soc") || lower.Contains("ciso") || lower.Contains("compliance"))
            return "\"VP of Information Security\" OR \"CISO\"";

        if (lower.Contains("cloud") || lower.Contains("azure") || lower.Contains("aws") || lower.Contains("migration"))
            return "\"VP of Cloud\" OR \"Head of Infrastructure\"";

        if (lower.Contains("devops") || lower.Contains("platform") || lower.Contains("sre"))
            return "\"Head of DevOps\" OR \"Director of Platform Engineering\"";

        return "\"VP of Engineering\" OR \"CTO\"";
    }

    private static DiscoveredExecutive FallbackExecutive(string companyName, string targetTitleQuery)
    {
        var titleDisplay = targetTitleQuery.Replace("\"", "").Replace(" OR ", " / ");
        var searchUrl = $"https://www.linkedin.com/search/results/people/?keywords={Uri.EscapeDataString(companyName)}%20{Uri.EscapeDataString(titleDisplay.Split('/')[0].Trim())}";
        return new DiscoveredExecutive("Hiring Leader (Review on LinkedIn)", titleDisplay, searchUrl);
    }
}
