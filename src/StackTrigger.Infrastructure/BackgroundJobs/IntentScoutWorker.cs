using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Application.Digests.Commands.SendWeeklyDigests;
using StackTrigger.Application.IntentSignals.Commands.ProcessPendingSignals;

namespace StackTrigger.Infrastructure.BackgroundJobs;

public sealed class IntentScoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntentScoutWorker> _logger;
    private readonly TimeSpan _period;
    private DateTime? _lastWeeklyDigestDate;

    public IntentScoutWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<IntentScoutWorker> logger,
        TimeSpan? period = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _period = period ?? TimeSpan.FromHours(6);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IntentScoutWorker started. Cycle period: {Period}", _period);

        // Immediate pass upon startup
        await ExecuteScrapeCycleAsync(stoppingToken);

        using var timer = new PeriodicTimer(_period);
        try
        {
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExecuteScrapeCycleAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("IntentScoutWorker execution cancelled gracefully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error occurred within IntentScoutWorker loop.");
        }
    }

    public async Task ExecuteScrapeCycleAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scrape and dispatch cycle at {Time}", DateTimeOffset.UtcNow);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scraper = scope.ServiceProvider.GetRequiredService<IIntentScraper>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Phase 1: Scrape ATS targets
            var detectedSignals = await scraper.ScrapeSignalsAsync(cancellationToken);
            int totalProcessed = 0;
            int newSaved = 0;

            foreach (var command in detectedSignals)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                totalProcessed++;
                var result = await mediator.Send(command, cancellationToken);
                if (result.Saved)
                {
                    newSaved++;
                }
            }

            _logger.LogInformation(
                "Completed scrape phase. Discovered: {Total}, Newly Saved: {Saved}, Skipped Duplicates: {Skipped}",
                totalProcessed,
                newSaved,
                totalProcessed - newSaved);

            // Phase 2: Dispatch pending signals to subscribed client webhooks (Slack / Teams)
            var dispatchResult = await mediator.Send(new ProcessPendingSignalsCommand(), cancellationToken);
            _logger.LogInformation(
                "Completed dispatch phase. Processed: {Total}, Successfully Pushed: {Success}, Failed: {Failed}",
                dispatchResult.TotalProcessed,
                dispatchResult.SuccessfullyDispatched,
                dispatchResult.FailedCount);

            // Phase 3: Automated Friday Afternoon Weekly Value Digest Check
            if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Friday && _lastWeeklyDigestDate?.Date != DateTime.UtcNow.Date)
            {
                _logger.LogInformation("Friday detected. Initiating automated Weekly Value Digest dispatch...");
                var digestResult = await mediator.Send(new SendWeeklyDigestsCommand(), cancellationToken);
                _lastWeeklyDigestDate = DateTime.UtcNow.Date;
                _logger.LogInformation(
                    "Weekly Digest dispatch completed. Delivered to {Count} active client firms.",
                    digestResult.TotalDelivered);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Scrape/dispatch cycle cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception during scrape/dispatch cycle execution.");
        }
    }
}
