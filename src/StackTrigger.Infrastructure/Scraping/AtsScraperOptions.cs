namespace StackTrigger.Infrastructure.Scraping;

public sealed class AtsScraperOptions
{
    public const string SectionName = "AtsScraper";

    public List<string> TargetUrls { get; set; } = [];

    public List<string> MonitoredKeywords { get; set; } =
    [
        "Azure",
        "AWS",
        "Cloud Migration",
        ".NET",
        ".NET 9",
        "Clean Architecture",
        "SOC2",
        "Cybersecurity",
        "Kubernetes",
        "Terraform",
        "CrowdStrike",
        "Zero Trust"
    ];

    public string UserAgent { get; set; } = "StackTriggerScout/1.0 (+https://stacktrigger.com/bot)";
}

