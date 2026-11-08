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
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providedKey) || providedKey != _configuredAdminKey)
        {
            return Unauthorized(new { error = "Invalid or missing founder admin key." });
        }

        var metrics = await _mediator.Send(new GetAdminMetricsQuery(), cancellationToken);
        return Ok(metrics);
    }
}

