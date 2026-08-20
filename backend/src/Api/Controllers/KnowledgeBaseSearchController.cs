using Application.KnowledgeBase.Queries.SearchKnowledgeBase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/kb/search")]
public sealed class KnowledgeBaseSearchController : ControllerBase
{
    private readonly ISender _sender;

    public KnowledgeBaseSearchController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int limit = 5, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchKnowledgeBaseQuery(query, limit), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "Forbidden" => Problem(title: "Forbidden", detail: result.ErrorMessage, statusCode: StatusCodes.Status403Forbidden),
                "ValidationFailed" => Problem(title: "Validation failed", detail: result.ErrorMessage, statusCode: StatusCodes.Status400BadRequest),
                "EmbeddingFailed" => Problem(title: "Bad request", detail: result.ErrorMessage, statusCode: StatusCodes.Status400BadRequest),
                _ => Problem(title: "Bad request", detail: result.ErrorMessage, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }
}