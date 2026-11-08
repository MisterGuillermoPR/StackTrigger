using MediatR;
using Microsoft.AspNetCore.Mvc;
using StackTrigger.Application.Metrics.Queries.GetAdminMetrics;

namespace StackTrigger.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private const string AdminSecretKey = "st_live_founder_admin_secret_2026";

    public AdminController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(
        [FromHeader(Name = "X-Admin-Key")] string? providedKey,
        CancellationToken cancellationToken)
    {
        // Simple secure header-based authentication for founder access
        if (string.IsNullOrWhiteSpace(providedKey) || providedKey != AdminSecretKey)
        {
            return Unauthorized(new { error = "Invalid or missing founder admin key." });
        }

        var metrics = await _mediator.Send(new GetAdminMetricsQuery(), cancellationToken);
        return Ok(metrics);
    }
}
