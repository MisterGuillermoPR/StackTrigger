using MediatR;
using Microsoft.Extensions.Logging;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Repositories;

namespace StackTrigger.Application.Subscriptions.Commands.CreateSubscription;

public sealed class CreateSubscriptionCommandHandler
    : IRequestHandler<CreateSubscriptionCommand, CreateSubscriptionResult>
{
    private readonly IClientSubscriptionRepository _repository;
    private readonly IWebhookDeliveryService _webhookDeliveryService;
    private readonly ILogger<CreateSubscriptionCommandHandler> _logger;

    public CreateSubscriptionCommandHandler(
        IClientSubscriptionRepository repository,
        IWebhookDeliveryService webhookDeliveryService,
        ILogger<CreateSubscriptionCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _webhookDeliveryService = webhookDeliveryService ?? throw new ArgumentNullException(nameof(webhookDeliveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CreateSubscriptionResult> Handle(
        CreateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        try
        {
            // 1. Instantiate via DDD factory (validates companyName, webhook URL format, keywords)
            var subscription = ClientSubscription.Create(
                companyName: request.CompanyName,
                webhookUrl: request.WebhookUrl,
                targetKeywords: request.TargetKeywords,
                isActive: true
            );

            // 2. Persist to PostgreSQL database
            await _repository.AddAsync(subscription, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created ClientSubscription {Id} for '{Company}' with keywords: [{Keywords}]",
                subscription.Id,
                subscription.CompanyName,
                string.Join(", ", subscription.TargetKeywords));

            // 3. Send immediate test lead to their Slack so they see proof of connection
            var verificationSignal = IntentSignal.Create(
                companyName: "StackTrigger Verification Bot",
                jobTitle: "Welcome to Live Intent Stream",
                targetTechnologies: request.TargetKeywords.Take(3).ToList(),
                sourceUrl: "https://stacktrigger.com"
            );

            _ = _webhookDeliveryService.DeliverSignalAsync(verificationSignal, subscription.WebhookUrl, cancellationToken);

            return new CreateSubscriptionResult(true, subscription.Id, "Subscription activated and test alert delivered.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid subscription parameters provided.");
            return new CreateSubscriptionResult(false, null, ex.Message);
        }
    }
}
