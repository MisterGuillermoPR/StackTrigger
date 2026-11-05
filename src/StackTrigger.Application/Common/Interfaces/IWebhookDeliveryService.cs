using StackTrigger.Domain.Entities;

namespace StackTrigger.Application.Common.Interfaces;

public interface IWebhookDeliveryService
{
    Task<bool> DeliverSignalAsync(IntentSignal signal, string webhookUrl, CancellationToken cancellationToken);
}

