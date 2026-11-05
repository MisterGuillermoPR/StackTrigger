using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Application.Common.Options;
using StackTrigger.Domain.Repositories;

namespace StackTrigger.Application.IntentSignals.Commands.ProcessPendingSignals;

public sealed class ProcessPendingSignalsCommandHandler
    : IRequestHandler<ProcessPendingSignalsCommand, ProcessPendingSignalsResult>
{
    private readonly IIntentSignalRepository _intentSignalRepository;
    private readonly IClientSubscriptionRepository _clientSubscriptionRepository;
    private readonly IWebhookDeliveryService _webhookDeliveryService;
    private readonly WebhookDispatcherOptions _options;
    private readonly ILogger<ProcessPendingSignalsCommandHandler> _logger;

    public ProcessPendingSignalsCommandHandler(
        IIntentSignalRepository intentSignalRepository,
        IClientSubscriptionRepository clientSubscriptionRepository,
        IWebhookDeliveryService webhookDeliveryService,
        IOptions<WebhookDispatcherOptions> options,
        ILogger<ProcessPendingSignalsCommandHandler> logger)
    {
        _intentSignalRepository = intentSignalRepository ?? throw new ArgumentNullException(nameof(intentSignalRepository));
        _clientSubscriptionRepository = clientSubscriptionRepository ?? throw new ArgumentNullException(nameof(clientSubscriptionRepository));
        _webhookDeliveryService = webhookDeliveryService ?? throw new ArgumentNullException(nameof(webhookDeliveryService));
        _options = options?.Value ?? new WebhookDispatcherOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProcessPendingSignalsResult> Handle(
        ProcessPendingSignalsCommand request,
        CancellationToken cancellationToken)
    {
        var batchSize = request.BatchLimit ?? _options.BatchSize;

        _logger.LogInformation(
            "Fetching up to {BatchSize} pending signals and active multi-tenant subscriptions...",
            batchSize);

        var pendingSignals = await _intentSignalRepository.GetPendingSignalsAsync(batchSize, cancellationToken);
        if (pendingSignals.Count == 0)
        {
            _logger.LogInformation("No pending intent signals found for dispatch.");
            return new ProcessPendingSignalsResult(0, 0, 0);
        }

        var activeClients = await _clientSubscriptionRepository.GetActiveSubscriptionsAsync(cancellationToken);
        if (activeClients.Count == 0)
        {
            _logger.LogWarning("No active client subscriptions registered. Skipping dispatch cycle.");
            return new ProcessPendingSignalsResult(pendingSignals.Count, 0, 0);
        }

        int successfullyDispatched = 0;
        int failedCount = 0;

        foreach (var signal in pendingSignals)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Keyword router: filter clients whose TargetKeywords match this signal
            var matchingClients = activeClients.Where(client => client.MatchesSignal(signal)).ToList();

            if (matchingClients.Count == 0)
            {
                _logger.LogInformation(
                    "Signal {SignalId} ({Company} - {Title}) matched 0 subscribed clients. Marking as dispatched.",
                    signal.Id,
                    signal.CompanyName,
                    signal.JobTitle);

                // No subscribers interested in this specific tech stack; mark dispatched to prevent reprocessing
                signal.MarkAsDispatched();
                await _intentSignalRepository.UpdateAsync(signal, cancellationToken);
                continue;
            }

            bool allClientDeliveriesSucceeded = true;

            foreach (var client in matchingClients)
            {
                _logger.LogInformation(
                    "Routing signal {SignalId} to client '{ClientName}' at webhook: {Webhook}",
                    signal.Id,
                    client.CompanyName,
                    client.WebhookUrl);

                var delivered = await _webhookDeliveryService.DeliverSignalAsync(
                    signal,
                    client.WebhookUrl,
                    cancellationToken);

                if (delivered)
                {
                    successfullyDispatched++;
                }
                else
                {
                    failedCount++;
                    allClientDeliveriesSucceeded = false;
                }
            }

            if (allClientDeliveriesSucceeded)
            {
                signal.MarkAsDispatched();
            }
            else
            {
                signal.MarkAsFailed();
            }

            await _intentSignalRepository.UpdateAsync(signal, cancellationToken);
        }

        await _intentSignalRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Multi-tenant dispatch completed. Signals: {Total}, Client Deliveries Succeeded: {Success}, Failed: {Failed}",
            pendingSignals.Count,
            successfullyDispatched,
            failedCount);

        return new ProcessPendingSignalsResult(pendingSignals.Count, successfullyDispatched, failedCount);
    }
}
