using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Models;

namespace StackTrigger.Application.Common.Interfaces;

public interface IWebhookDeliveryService
{
    Task<bool> DeliverSignalAsync(IntentSignal signal, string webhookUrl, CancellationToken cancellationToken);
    Task<bool> DeliverWeeklyDigestAsync(WeeklyDigestSummary digest, string webhookUrl, CancellationToken cancellationToken = default);
}

