using MediatR;

namespace StackTrigger.Application.Subscriptions.Commands.CreateSubscription;

public sealed record CreateSubscriptionCommand(
    string CompanyName,
    string WebhookUrl,
    IReadOnlyList<string> TargetKeywords
) : IRequest<CreateSubscriptionResult>;

public sealed record CreateSubscriptionResult(
    bool Success,
    Guid? SubscriptionId,
    string Message
);

