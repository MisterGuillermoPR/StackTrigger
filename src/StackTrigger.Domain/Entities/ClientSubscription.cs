namespace StackTrigger.Domain.Entities;

public sealed class ClientSubscription
{
    private readonly List<string> _targetKeywords = [];

    // Required by EF Core
    private ClientSubscription()
    {
        CompanyName = null!;
        WebhookUrl = null!;
    }

    private ClientSubscription(
        Guid id,
        string companyName,
        string webhookUrl,
        IEnumerable<string> targetKeywords,
        bool isActive)
    {
        Id = id;
        CompanyName = companyName;
        WebhookUrl = webhookUrl;
        IsActive = isActive;
        _targetKeywords = targetKeywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Guid Id { get; private set; }
    public string CompanyName { get; private set; }
    public string WebhookUrl { get; private set; }
    public IReadOnlyCollection<string> TargetKeywords => _targetKeywords.AsReadOnly();
    public bool IsActive { get; private set; }

    public static ClientSubscription Create(
        string companyName,
        string webhookUrl,
        IEnumerable<string> targetKeywords,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyName, nameof(companyName));
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookUrl, nameof(webhookUrl));

        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var parsedUri) ||
            parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Webhook URL must be a valid, secure HTTPS URL.", nameof(webhookUrl));
        }

        // SSRF protection: reject loopback, internal IPs, and cloud metadata endpoints
        if (parsedUri.IsLoopback ||
            parsedUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            parsedUri.Host.Equals("169.254.169.254") ||
            parsedUri.Host.StartsWith("10.", StringComparison.OrdinalIgnoreCase) ||
            parsedUri.Host.StartsWith("192.168.", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Webhook URL must point to a public endpoint.", nameof(webhookUrl));
        }

        ArgumentNullException.ThrowIfNull(targetKeywords, nameof(targetKeywords));

        var keywordsList = targetKeywords.ToList();
        if (keywordsList.Count == 0 || keywordsList.All(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one target keyword must be specified.", nameof(targetKeywords));
        }

        return new ClientSubscription(
            id: Guid.NewGuid(),
            companyName: companyName.Trim(),
            webhookUrl: parsedUri.ToString(),
            targetKeywords: keywordsList,
            isActive: isActive
        );
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void UpdateWebhookUrl(string newWebhookUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newWebhookUrl, nameof(newWebhookUrl));
        if (!Uri.TryCreate(newWebhookUrl, UriKind.Absolute, out var parsedUri) ||
            parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Webhook URL must be a valid, secure HTTPS URL.", nameof(newWebhookUrl));
        }

        if (parsedUri.IsLoopback ||
            parsedUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            parsedUri.Host.Equals("169.254.169.254") ||
            parsedUri.Host.StartsWith("10.", StringComparison.OrdinalIgnoreCase) ||
            parsedUri.Host.StartsWith("192.168.", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Webhook URL must point to a public endpoint.", nameof(newWebhookUrl));
        }

        WebhookUrl = parsedUri.ToString();
    }

    public void UpdateTargetKeywords(IEnumerable<string> newKeywords)
    {
        ArgumentNullException.ThrowIfNull(newKeywords, nameof(newKeywords));
        var list = newKeywords.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("At least one target keyword must be specified.", nameof(newKeywords));
        }

        _targetKeywords.Clear();
        _targetKeywords.AddRange(list);
    }

    public bool MatchesSignal(IntentSignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal, nameof(signal));

        if (!IsActive)
        {
            return false;
        }

        // Match case-insensitively against signal's detected technologies
        return _targetKeywords.Any(clientKeyword =>
            signal.TargetTechnologies.Any(signalTech =>
                string.Equals(clientKeyword, signalTech, StringComparison.OrdinalIgnoreCase)) ||
            signal.JobTitle.Contains(clientKeyword, StringComparison.OrdinalIgnoreCase));
    }
}

