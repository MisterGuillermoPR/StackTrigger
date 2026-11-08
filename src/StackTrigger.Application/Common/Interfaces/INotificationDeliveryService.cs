namespace StackTrigger.Application.Common.Interfaces;

public interface INotificationDeliveryService
{
    Task<bool> SendSecurityCodeEmailAsync(string recipientEmail, string code, CancellationToken cancellationToken = default);
    Task<bool> SendSecurityCodeSmsAsync(string recipientPhone, string code, CancellationToken cancellationToken = default);
}

