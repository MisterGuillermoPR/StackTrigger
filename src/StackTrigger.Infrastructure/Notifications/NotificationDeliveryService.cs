using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackTrigger.Application.Common.Interfaces;

namespace StackTrigger.Infrastructure.Notifications;

public sealed class NotificationDeliveryService : INotificationDeliveryService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotificationDeliveryService> _logger;

    public NotificationDeliveryService(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationDeliveryService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendSecurityCodeEmailAsync(
        string recipientEmail,
        string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail, nameof(recipientEmail));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));

        // 1. Try Resend API (preferred modern REST email provider)
        var resendKey = _config["Resend:ApiKey"] ?? Environment.GetEnvironmentVariable("RESEND_API_KEY");
        if (!string.IsNullOrWhiteSpace(resendKey))
        {
            return await SendViaResendAsync(resendKey, recipientEmail, code, cancellationToken);
        }

        // 2. Try Standard SMTP (Gmail, Google Workspace, Porkbun, Outlook)
        var smtpHost = _config["Smtp:Host"] ?? Environment.GetEnvironmentVariable("SMTP_HOST");
        if (!string.IsNullOrWhiteSpace(smtpHost))
        {
            return await SendViaSmtpAsync(smtpHost, recipientEmail, code, cancellationToken);
        }

        _logger.LogWarning(
            "[SECURITY ALERT] No email provider configured (Resend or SMTP). Verification code for {Email} is: {Code}. Configure 'Resend:ApiKey' or 'Smtp:Host' in appsettings or env variables to send real emails.",
            recipientEmail,
            code);

        return false;
    }

    public async Task<bool> SendSecurityCodeSmsAsync(
        string recipientPhone,
        string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientPhone, nameof(recipientPhone));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));

        var accountSid = _config["Twilio:AccountSid"] ?? Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        var authToken = _config["Twilio:AuthToken"] ?? Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        var fromNumber = _config["Twilio:FromNumber"] ?? Environment.GetEnvironmentVariable("TWILIO_FROM_NUMBER");

        if (string.IsNullOrWhiteSpace(accountSid) ||
            string.IsNullOrWhiteSpace(authToken) ||
            string.IsNullOrWhiteSpace(fromNumber))
        {
            _logger.LogWarning(
                "[SECURITY ALERT] Twilio SMS credentials not set. Code for {Phone} is: {Code}. Configure TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN, and TWILIO_FROM_NUMBER to send real SMS.",
                recipientPhone,
                code);
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            var values = new Dictionary<string, string>
            {
                { "To", recipientPhone },
                { "From", fromNumber },
                { "Body", $"[StackTrigger] Your founder login verification code is: {code}. Expires in 5 minutes." }
            };

            var endpoint = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
            var response = await client.PostAsync(endpoint, new FormUrlEncodedContent(values), cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully dispatched real SMS verification code to {Phone}.", recipientPhone);
                return true;
            }

            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Twilio SMS delivery failed: {Error}", err);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver SMS via Twilio.");
            return false;
        }
    }

    private async Task<bool> SendViaResendAsync(
        string apiKey,
        string recipientEmail,
        string code,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var fromEmail = _config["Resend:FromEmail"] ?? Environment.GetEnvironmentVariable("RESEND_FROM_EMAIL") ?? "security@stacktrigger.com";

            var body = new
            {
                from = $"StackTrigger Security <{fromEmail}>",
                to = new[] { recipientEmail },
                subject = $"StackTrigger Founder Security Code: {code}",
                html = $@"
                    <div style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; max-width: 500px; margin: 0 auto; padding: 24px; background: #0B0F19; color: #F8FAFC; border-radius: 8px; border: 1px solid #1E293B;'>
                        <h2 style='color: #FFFFFF; margin-top: 0;'>Founder Authentication</h2>
                        <p style='color: #94A3B8; font-size: 14px;'>A login attempt was initiated for the StackTrigger Founder Console.</p>
                        <div style='background: #0F172A; border: 1px solid #334155; padding: 16px; border-radius: 6px; text-align: center; margin: 20px 0;'>
                            <span style='font-size: 32px; font-weight: bold; font-family: monospace; letter-spacing: 6px; color: #3B82F6;'>{code}</span>
                        </div>
                        <p style='color: #64748B; font-size: 12px;'>This code expires in 5 minutes. If you did not initiate this request, lock your session immediately.</p>
                    </div>"
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.resend.com/emails", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Real email successfully sent to {Email} via Resend.", recipientEmail);
                return true;
            }

            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Resend email failed: {Error}", err);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via Resend.");
            return false;
        }
    }

    private Task<bool> SendViaSmtpAsync(
        string host,
        string recipientEmail,
        string code,
        CancellationToken cancellationToken)
    {
        try
        {
            var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
            var username = _config["Smtp:Username"] ?? Environment.GetEnvironmentVariable("SMTP_USERNAME");
            var password = _config["Smtp:Password"] ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD");
            var fromEmail = _config["Smtp:FromEmail"] ?? Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? username ?? "security@stacktrigger.com";

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, "StackTrigger Security"),
                Subject = $"Your StackTrigger Verification Code: {code}",
                Body = $"Your 6-digit one-time login verification code is: {code}\n\nThis code expires in 5 minutes.",
                IsBodyHtml = false
            };
            message.To.Add(recipientEmail);

            client.Send(message);
            _logger.LogInformation("Real email successfully delivered to {Email} via SMTP ({Host}).", recipientEmail, host);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SMTP ({Host}).", host);
            return Task.FromResult(false);
        }
    }
}

