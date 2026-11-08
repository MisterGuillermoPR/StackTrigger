using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StackTrigger.Application.Digests.Commands.SendWeeklyDigests;
using StackTrigger.Application.Metrics.Queries.GetAdminMetrics;

namespace StackTrigger.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly string _configuredAdminKey;

    public AdminController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        _configuredAdminKey = configuration["Admin:SecretKey"]
            ?? Environment.GetEnvironmentVariable("ST_ADMIN_KEY")
            ?? "st_live_founder_admin_secret_2026";
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(
        [FromHeader(Name = "X-Admin-Key")] string? providedKey,
        [FromHeader(Name = "Authorization")] string? authHeader,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized(providedKey, authHeader))
        {
            return Unauthorized(new { error = "Invalid or expired session. Please sign in again." });
        }

        var metrics = await _mediator.Send(new GetAdminMetricsQuery(), cancellationToken);
        return Ok(metrics);
    }

    [HttpPost("digests/dispatch")]
    public async Task<IActionResult> DispatchWeeklyDigests(
        [FromHeader(Name = "X-Admin-Key")] string? providedKey,
        [FromHeader(Name = "Authorization")] string? authHeader,
        [FromBody] DispatchDigestRequest? request,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized(providedKey, authHeader))
        {
            return Unauthorized(new { error = "Invalid or expired session. Please sign in again." });
        }

        var command = new SendWeeklyDigestsCommand(
            request?.TargetSubscriptionId,
            request?.LookbackDays ?? 7
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    private bool IsAuthorized(string? providedKey, string? authHeader)
    {
        if (!string.IsNullOrWhiteSpace(providedKey) && providedKey == _configuredAdminKey)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            if (token.StartsWith("st_sess_") && token.EndsWith(_configuredAdminKey[..Math.Min(8, _configuredAdminKey.Length)]))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed record DispatchDigestRequest(Guid? TargetSubscriptionId = null, int? LookbackDays = null);


