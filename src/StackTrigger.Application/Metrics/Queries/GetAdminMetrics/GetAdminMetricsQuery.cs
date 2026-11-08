using MediatR;

namespace StackTrigger.Application.Metrics.Queries.GetAdminMetrics;

public sealed record GetAdminMetricsQuery : IRequest<AdminMetricsDto>;

public sealed record AdminMetricsDto(
    int TotalLeadsScraped,
    int PendingLeads,
    int DispatchedLeads,
    int ActiveSubscribers,
    decimal MonthlyRecurringRevenue,
    IReadOnlyList<ClientSubscriptionSummaryDto> Subscribers,
    IReadOnlyList<RecentLeadDto> RecentLeads
);

public sealed record ClientSubscriptionSummaryDto(
    Guid Id,
    string CompanyName,
    string WebhookUrl,
    IReadOnlyList<string> TargetKeywords,
    bool IsActive
);

public sealed record RecentLeadDto(
    Guid Id,
    string CompanyName,
    string JobTitle,
    IReadOnlyList<string> TargetTechnologies,
    string SourceUrl,
    string DispatchStatus,
    DateTimeOffset DateDetected,
    string? DecisionMakerTitle
);

