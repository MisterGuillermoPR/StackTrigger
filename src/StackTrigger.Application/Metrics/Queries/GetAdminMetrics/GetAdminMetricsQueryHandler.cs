using MediatR;
using StackTrigger.Domain.Enums;
using StackTrigger.Domain.Repositories;

namespace StackTrigger.Application.Metrics.Queries.GetAdminMetrics;

public sealed class GetAdminMetricsQueryHandler
    : IRequestHandler<GetAdminMetricsQuery, AdminMetricsDto>
{
    private readonly IIntentSignalRepository _signalRepository;
    private readonly IClientSubscriptionRepository _subscriptionRepository;

    public GetAdminMetricsQueryHandler(
        IIntentSignalRepository signalRepository,
        IClientSubscriptionRepository subscriptionRepository)
    {
        _signalRepository = signalRepository ?? throw new ArgumentNullException(nameof(signalRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
    }

    public async Task<AdminMetricsDto> Handle(
        GetAdminMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var totalLeads = await _signalRepository.GetCountAsync(null, cancellationToken);
        var pending = await _signalRepository.GetCountAsync(DispatchStatus.Pending, cancellationToken);
        var dispatched = await _signalRepository.GetCountAsync(DispatchStatus.Dispatched, cancellationToken);

        var subscribers = await _subscriptionRepository.GetAllAsync(cancellationToken);

        var activeCount = subscribers.Count(s => s.IsActive);
        var mrr = activeCount * 400.00m; // $400/month per active client

        var recentSignals = await _signalRepository.GetRecentSignalsAsync(15, cancellationToken);

        var recentSignalDtos = recentSignals.Select(s => new RecentLeadDto(
            s.Id,
            s.CompanyName,
            s.JobTitle,
            s.TargetTechnologies.ToList(),
            s.SourceUrl,
            s.DispatchStatus.ToString(),
            s.DateDetected,
            s.DecisionMakerTitle
        )).ToList();

        var subDtos = subscribers.Select(s => new ClientSubscriptionSummaryDto(
            s.Id,
            s.CompanyName,
            MaskWebhookUrl(s.WebhookUrl),
            s.TargetKeywords.ToList(),
            s.IsActive
        )).ToList();

        return new AdminMetricsDto(
            totalLeads,
            pending,
            dispatched,
            activeCount,
            mrr,
            subDtos,
            recentSignalDtos
        );
    }

    private static string MaskWebhookUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length <= 35)
        {
            return "https://hooks.slack.com/services/***";
        }

        // Show the first 35 chars (service base) and redact the secret token
        return string.Concat(url.AsSpan(0, 35), "...***");
    }
}
