using Application.Users.Queries.ListAgents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("agents")]
    public async Task<IActionResult> ListAgents(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListAgentsQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "Forbidden" => Problem(title: "Forbidden", detail: result.ErrorMessage!, statusCode: StatusCodes.Status403Forbidden),
                _ => Problem(title: "Bad request", detail: result.ErrorMessage!, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }
}
