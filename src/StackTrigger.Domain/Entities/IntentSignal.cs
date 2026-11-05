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
        DispatchStatus dispatchStatus)
    {
        Id = id;
        CompanyName = companyName;
        JobTitle = jobTitle;
        SourceUrl = sourceUrl;
        DateDetected = dateDetected;
        DispatchStatus = dispatchStatus;
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

    public static IntentSignal Create(
        string companyName,
        string jobTitle,
        IEnumerable<string> targetTechnologies,
        string sourceUrl,
        DateTimeOffset? dateDetected = null)
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

        return new IntentSignal(
            id: Guid.NewGuid(),
            companyName: companyName.Trim(),
            jobTitle: jobTitle.Trim(),
            targetTechnologies: techList,
            sourceUrl: parsedUri.ToString(),
            dateDetected: dateDetected ?? DateTimeOffset.UtcNow,
            dispatchStatus: DispatchStatus.Pending
        );
    }

    public void MarkAsDispatched()
    {
        DispatchStatus = DispatchStatus.Dispatched;
    }

    public void MarkAsFailed()
    {
        DispatchStatus = DispatchStatus.Failed;
    }

    public void ResetToPending()
    {
        DispatchStatus = DispatchStatus.Pending;
    }
}

