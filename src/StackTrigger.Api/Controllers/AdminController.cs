using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        var isAuthorized = false;

        // 1. Direct key match
        if (!string.IsNullOrWhiteSpace(providedKey) && providedKey == _configuredAdminKey)
        {
            isAuthorized = true;
        }

        // 2. Bearer session token match
        if (!isAuthorized && !string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            if (token.StartsWith("st_sess_") && token.EndsWith(_configuredAdminKey[..Math.Min(8, _configuredAdminKey.Length)]))
            {
                isAuthorized = true;
            }
        }

        if (!isAuthorized)
        {
            return Unauthorized(new { error = "Invalid or expired session. Please sign in again." });
        }

        var metrics = await _mediator.Send(new GetAdminMetricsQuery(), cancellationToken);
        return Ok(metrics);
    }
}

