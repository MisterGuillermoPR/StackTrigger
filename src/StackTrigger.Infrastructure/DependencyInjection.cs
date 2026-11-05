using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Domain.Repositories;
using StackTrigger.Infrastructure.BackgroundJobs;
using StackTrigger.Infrastructure.Persistence;
using StackTrigger.Infrastructure.Persistence.Repositories;
using StackTrigger.Infrastructure.Scraping;
using StackTrigger.Infrastructure.Webhooks;

namespace StackTrigger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Persistence
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=stacktrigger;Username=postgres;Password=postgres";

        services.AddDbContext<StackTriggerDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IIntentSignalRepository, IntentSignalRepository>();
        services.AddScoped<IClientSubscriptionRepository, ClientSubscriptionRepository>();

        // Scraping Configuration
        services.Configure<AtsScraperOptions>(
            configuration.GetSection(AtsScraperOptions.SectionName));

        // Register HTML Scraper as internal service
        services.AddHttpClient<AtsHtmlScraper>()
            .AddResilienceHandler("ats-html-pipeline", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                });
                builder.AddTimeout(TimeSpan.FromSeconds(30));
            });

        // Register Primary Enterprise ATS Scraper (Greenhouse + Lever REST API + HTML Fallback)
        services.AddHttpClient<IIntentScraper, EnterpriseAtsScraper>()
            .AddResilienceHandler("ats-api-pipeline", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                });
                builder.AddTimeout(TimeSpan.FromSeconds(25));
            });

        // Webhook Delivery Service
        services.AddHttpClient<IWebhookDeliveryService, SlackWebhookDeliveryService>()
            .AddResilienceHandler("slack-webhook-pipeline", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                });

                builder.AddTimeout(TimeSpan.FromSeconds(15));
            });

        // Background worker
        services.AddHostedService<IntentScoutWorker>();

        return services;
    }
}

