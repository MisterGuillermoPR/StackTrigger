using StackTrigger.Domain.Enums;

namespace StackTrigger.Domain.Entities;

public sealed class IntentSignal
{
    private readonly List<string> _targetTechnologies = [];

    // Required by EF Core
    private IntentSignal()
    {
        CompanyName = null!;
        JobTitle = null!;
        SourceUrl = null!;
    }

    private IntentSignal(
        Guid id,
        string companyName,
        string jobTitle,
        IEnumerable<string> targetTechnologies,
        string sourceUrl,
        DateTimeOffset dateDetected,
        DispatchStatus dispatchStatus,
        string? salaryRange,
        string? decisionMakerTitle,
        string? linkedInSearchUrl,
        string? outreachIcebreaker)
    {
        Id = id;
        CompanyName = companyName;
        JobTitle = jobTitle;
        SourceUrl = sourceUrl;
        DateDetected = dateDetected;
        DispatchStatus = dispatchStatus;
        SalaryRange = salaryRange;
        DecisionMakerTitle = decisionMakerTitle;
        LinkedInSearchUrl = linkedInSearchUrl;
        OutreachIcebreaker = outreachIcebreaker;

        _targetTechnologies = targetTechnologies
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Guid Id { get; private set; }
    public string CompanyName { get; private set; }
    public string JobTitle { get; private set; }
    public IReadOnlyCollection<string> TargetTechnologies => _targetTechnologies.AsReadOnly();
    public string SourceUrl { get; private set; }
    public DateTimeOffset DateDetected { get; private set; }
    public DispatchStatus DispatchStatus { get; private set; }

    // Enriched Intelligence fields
    public string? SalaryRange { get; private set; }
    public string? DecisionMakerTitle { get; private set; }
    public string? LinkedInSearchUrl { get; private set; }
    public string? OutreachIcebreaker { get; private set; }

    public static IntentSignal Create(
        string companyName,
        string jobTitle,
        IEnumerable<string> targetTechnologies,
        string sourceUrl,
        DateTimeOffset? dateDetected = null,
        string? salaryRange = null,
        string? decisionMakerTitle = null,
        string? linkedInSearchUrl = null,
        string? outreachIcebreaker = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyName, nameof(companyName));
        ArgumentException.ThrowIfNullOrWhiteSpace(jobTitle, nameof(jobTitle));
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUrl, nameof(sourceUrl));

        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Source URL must be a valid absolute HTTP or HTTPS URL.", nameof(sourceUrl));
        }

        ArgumentNullException.ThrowIfNull(targetTechnologies, nameof(targetTechnologies));

        var techList = targetTechnologies.ToList();
        if (techList.Count == 0 || techList.All(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one target technology must be specified.", nameof(targetTechnologies));
        }

        // Auto-generate decision maker title & 1-click LinkedIn prospecting link if not provided
        var resolvedDecisionMaker = decisionMakerTitle ?? InferDecisionMakerTitle(jobTitle);
        var resolvedLinkedInUrl = linkedInSearchUrl ?? GenerateLinkedInSearchUrl(companyName.Trim(), resolvedDecisionMaker);
        var resolvedIcebreaker = outreachIcebreaker ?? GenerateOutreachIcebreaker(companyName.Trim(), jobTitle.Trim(), techList);

        return new IntentSignal(
            id: Guid.NewGuid(),
            companyName: companyName.Trim(),
            jobTitle: jobTitle.Trim(),
            targetTechnologies: techList,
            sourceUrl: parsedUri.ToString(),
            dateDetected: dateDetected ?? DateTimeOffset.UtcNow,
            dispatchStatus: DispatchStatus.Pending,
            salaryRange: salaryRange?.Trim(),
            decisionMakerTitle: resolvedDecisionMaker,
            linkedInSearchUrl: resolvedLinkedInUrl,
            outreachIcebreaker: resolvedIcebreaker
        );
    }

    public void MarkAsDispatched() => DispatchStatus = DispatchStatus.Dispatched;
    public void MarkAsFailed() => DispatchStatus = DispatchStatus.Failed;
    public void ResetToPending() => DispatchStatus = DispatchStatus.Pending;

    public static string InferDecisionMakerTitle(string jobTitle)
    {
        var lower = jobTitle.ToLowerInvariant();

        if (lower.Contains("security") || lower.Contains("soc") || lower.Contains("ciso") || lower.Contains("compliance"))
            return "VP of Information Security / CISO";

        if (lower.Contains("cloud") || lower.Contains("architect") || lower.Contains("migration") || lower.Contains("azure") || lower.Contains("aws"))
            return "VP of Cloud Architecture / Head of Infrastructure";

        if (lower.Contains("devops") || lower.Contains("sre") || lower.Contains("platform") || lower.Contains("reliability"))
            return "Head of DevOps & Platform Engineering";

        if (lower.Contains("data") || lower.Contains("finops") || lower.Contains("analytics"))
            return "VP of Data Engineering / Head of FinOps";

        return "VP of Engineering / CTO";
    }

    public static string GenerateLinkedInSearchUrl(string companyName, string targetTitle)
    {
        var encodedCompany = Uri.EscapeDataString(companyName);
        var encodedTitle = Uri.EscapeDataString(targetTitle.Split('/')[0].Trim());
        return $"https://www.linkedin.com/search/results/people/?keywords={encodedCompany}%20{encodedTitle}";
    }

    public static string GenerateOutreachIcebreaker(string companyName, string jobTitle, IReadOnlyList<string> tech)
    {
        var techSummary = string.Join(" and ", tech.Take(2));
        return $"\"Saw {companyName} is actively hiring for a {jobTitle} focusing on {techSummary}. Most engineering leaders experience a delivery bottleneck while staffing this internally—are you exploring specialized partner capacity to accelerate the rollout?\"";
    }
}
