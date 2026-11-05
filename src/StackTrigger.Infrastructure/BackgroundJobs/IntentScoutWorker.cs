using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackTrigger.Application.Common.Interfaces;

namespace StackTrigger.Infrastructure.BackgroundJobs;

public sealed class IntentScoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntentScoutWorker> _logger;
    private readonly TimeSpan _period;

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

        // Run an immediate pass upon startup
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
        _logger.LogInformation("Starting scrape cycle at {Time}", DateTimeOffset.UtcNow);

        try
        {
            // Worker is a Singleton; MediatR and Scrapers/Repositories are Scoped.
            // Create an explicit scope to resolve dependencies.
            using var scope = _scopeFactory.CreateScope();
            var scraper = scope.ServiceProvider.GetRequiredService<IIntentScraper>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

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
                "Completed scrape cycle. Discovered: {Total}, Newly Saved: {Saved}, Skipped Duplicates: {Skipped}",
                totalProcessed,
                newSaved,
                totalProcessed - newSaved);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Scrape cycle cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception during scrape cycle execution.");
        }
    }
}
