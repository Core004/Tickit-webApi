using MediatR;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Common.Models;

namespace TicketSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??=
        HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value is null ? NoContent() : Ok(result.Value);
        }

        return result.Error?.Code switch
        {
            var code when code?.Contains("NotFound") == true => NotFound(result.Error),
            var code when code?.Contains("Validation") == true => BadRequest(result.Error),
            var code when code?.Contains("Unauthorized") == true => Unauthorized(result.Error),
            var code when code?.Contains("Forbidden") == true => Forbid(),
            var code when code?.Contains("Conflict") == true => Conflict(result.Error),
            _ => BadRequest(result.Error)
        };
    }

    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.Error?.Code switch
        {
            var code when code?.Contains("NotFound") == true => NotFound(result.Error),
            var code when code?.Contains("Validation") == true => BadRequest(result.Error),
            var code when code?.Contains("Unauthorized") == true => Unauthorized(result.Error),
            var code when code?.Contains("Forbidden") == true => Forbid(),
            var code when code?.Contains("Conflict") == true => Conflict(result.Error),
            _ => BadRequest(result.Error)
        };
    }
}
