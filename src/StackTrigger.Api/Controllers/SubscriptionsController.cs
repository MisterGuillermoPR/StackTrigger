using MediatR;
using Microsoft.AspNetCore.Mvc;
using StackTrigger.Application.Subscriptions.Commands.CreateSubscription;

namespace StackTrigger.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new { error = result.Message });
        }

        return Ok(new
        {
            success = true,
            subscriptionId = result.SubscriptionId,
            message = result.Message
        });
    }
}
