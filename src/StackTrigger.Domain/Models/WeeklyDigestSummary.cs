namespace StackTrigger.Domain.Models;

public sealed record WeeklyDigestSummary(
    string CompanyName,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int TotalSignalsDelivered,
    IReadOnlyDictionary<string, int> TopTechnologies,
    IReadOnlyList<string> TopTargetCompanies,
    decimal EstimatedPipelineValue
);
