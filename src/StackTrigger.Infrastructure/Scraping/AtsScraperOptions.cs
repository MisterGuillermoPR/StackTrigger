namespace StackTrigger.Infrastructure.Scraping;

public sealed class AtsScraperOptions
{
    public const string SectionName = "AtsScraper";

    // High-yield Enterprise ATS Company Slugs (Greenhouse & Lever)
    // Greenhouse: boards-api.greenhouse.io/v1/boards/{slug}/jobs?content=true
    public List<string> GreenhouseCompanies { get; set; } =
    [
        "gitlab",
        "cloudflare",
        "hashicorp",
        "elastic",
        "confluent",
        "stripe",
        "datadog",
        "snyk",
        "mongodb",
        "pagerduty",
        "okta",
        "twilio"
    ];

    // Lever: api.lever.co/v0/postings/{slug}?mode=json
    public List<string> LeverCompanies { get; set; } =
    [
        "palantir",
        "atlassian",
        "reddit",
        "netflix",
        "spotify",
        "figma"
    ];

    // Curated high-ticket engineering boards (HTML fallback)
    public List<string> TargetUrls { get; set; } =
    [
        "https://weworkremotely.com/categories/remote-devops-sysadmin-jobs",
        "https://weworkremotely.com/categories/remote-back-end-programming-jobs"
    ];

    // High-Value Intent Keywords that trigger cloud migrations, compliance, and agency work
    public List<string> MonitoredKeywords { get; set; } =
    [
        "Azure",
        "AWS",
        "GCP",
        "Cloud Migration",
        "Migrate to Cloud",
        ".NET",
        ".NET 9",
        "Clean Architecture",
        "SOC2",
        "SOC 2",
        "ISO27001",
        "CrowdStrike",
        "Zero Trust",
        "Kubernetes",
        "Terraform",
        "Datadog",
        "Snowflake",
        "FinOps"
    ];

    public string UserAgent { get; set; } = "StackTriggerScout/2.0 (+https://stacktrigger.com/bot)";
}
