using MediatR;
using Microsoft.Extensions.Logging;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Models;
using StackTrigger.Domain.Repositories;

namespace StackTrigger.Application.Digests.Commands.SendWeeklyDigests;

public sealed class SendWeeklyDigestsCommandHandler
    : IRequestHandler<SendWeeklyDigestsCommand, SendWeeklyDigestsResult>
{
    private readonly IClientSubscriptionRepository _subscriptionRepo;
    private readonly IIntentSignalRepository _signalRepo;
    private readonly IWebhookDeliveryService _webhookDeliveryService;
    private readonly ILogger<SendWeeklyDigestsCommandHandler> _logger;

    public SendWeeklyDigestsCommandHandler(
        IClientSubscriptionRepository subscriptionRepo,
        IIntentSignalRepository signalRepo,
        IWebhookDeliveryService webhookDeliveryService,
        ILogger<SendWeeklyDigestsCommandHandler> logger)
    {
        _subscriptionRepo = subscriptionRepo ?? throw new ArgumentNullException(nameof(subscriptionRepo));
        _signalRepo = signalRepo ?? throw new ArgumentNullException(nameof(signalRepo));
        _webhookDeliveryService = webhookDeliveryService ?? throw new ArgumentNullException(nameof(webhookDeliveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SendWeeklyDigestsResult> Handle(
        SendWeeklyDigestsCommand request,
        CancellationToken cancellationToken)
    {
        var lookback = request.LookbackDays > 0 ? request.LookbackDays : 7;
        var periodEnd = DateTimeOffset.UtcNow;
        var periodStart = periodEnd.AddDays(-lookback);

        _logger.LogInformation(
            "Generating Weekly Intent Digests for period {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
            periodStart,
            periodEnd);

        // 1. Fetch relevant subscriptions
        IReadOnlyList<ClientSubscription> subscriptions;
        if (request.TargetSubscriptionId.HasValue)
        {
            var single = await _subscriptionRepo.GetByIdAsync(request.TargetSubscriptionId.Value, cancellationToken);
            subscriptions = single is not null && single.IsActive ? new[] { single } : Array.Empty<ClientSubscription>();
        }
        else
        {
            subscriptions = await _subscriptionRepo.GetActiveSubscriptionsAsync(cancellationToken);
        }

        if (subscriptions.Count == 0)
        {
            _logger.LogInformation("No active client subscriptions to deliver weekly digests to.");
            return new SendWeeklyDigestsResult(0, 0, 0, Array.Empty<string>());
        }

        // 2. Fetch all signals detected during the period
        var periodSignals = await _signalRepo.GetSignalsForPeriodAsync(periodStart, periodEnd, cancellationToken);

        var deliveredFirms = new List<string>();
        var deliveredCount = 0;
        var failedCount = 0;

        foreach (var sub in subscriptions)
        {
            if (cancellationToken.IsCancellationRequested) break;

            // 3. Filter signals that matched this client's configured intent rules
            var matchedSignals = periodSignals
                .Where(s => sub.MatchesSignal(s))
                .ToList();

            // Calculate top technologies
            var topTech = matchedSignals
                .SelectMany(s => s.TargetTechnologies)
                .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToDictionary(g => g.Key, g => g.Count());

            // Calculate top enterprise target companies
            var topCompanies = matchedSignals
                .Select(s => s.CompanyName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();

            // Estimate potential consulting opportunity value ($25,000 average per qualified cloud/security project)
            var count = Math.Max(matchedSignals.Count, 1);
            var estimatedValue = count * 25000.00m;

            var digest = new WeeklyDigestSummary(
                sub.CompanyName,
                periodStart,
                periodEnd,
                matchedSignals.Count,
                topTech,
                topCompanies,
                estimatedValue
            );

            // 4. Deliver Block Kit digest to client's Slack webhook
            var sent = await _webhookDeliveryService.DeliverWeeklyDigestAsync(digest, sub.WebhookUrl, cancellationToken);
            if (sent)
            {
                deliveredCount++;
                deliveredFirms.Add(sub.CompanyName);
            }
            else
            {
                failedCount++;
            }
        }

        _logger.LogInformation(
            "Weekly Digest run complete. Delivered: {Delivered}/{Total}",
            deliveredCount,
            subscriptions.Count);

        return new SendWeeklyDigestsResult(
            subscriptions.Count,
            deliveredCount,
            failedCount,
            deliveredFirms
        );
    }
}
