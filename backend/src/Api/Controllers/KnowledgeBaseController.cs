using Application.KnowledgeBase.Commands.CreateKnowledgeDocument;
using Application.KnowledgeBase.Queries.GetKnowledgeDocumentDetail;
using Application.KnowledgeBase.Queries.GetKnowledgeDocuments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/kb/documents")]
public sealed class KnowledgeBaseController : ControllerBase
{
    private readonly ISender _sender;

    public KnowledgeBaseController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetKnowledgeDocumentsQuery(page, pageSize), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
    }

    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> GetDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetKnowledgeDocumentDetailQuery(documentId), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "DocumentNotFound" => NotFoundProblem(result.ErrorMessage!),
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDocument(CreateKnowledgeDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateKnowledgeDocumentCommand(request.Title, request.SourceFile, request.SourceType, request.Content), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return CreatedAtAction(nameof(GetDocument), new { documentId = result.Value!.Id }, result.Value);
    }

    private IActionResult BadRequestProblem(string detail)
    {
        return Problem(title: "Bad request", detail: detail, statusCode: StatusCodes.Status400BadRequest);
    }

    private IActionResult ValidationProblem(string detail)
    {
        return Problem(title: "Validation failed", detail: detail, statusCode: StatusCodes.Status400BadRequest);
    }

    private IActionResult NotFoundProblem(string detail)
    {
        return Problem(title: "Not found", detail: detail, statusCode: StatusCodes.Status404NotFound);
    }

    private IActionResult ForbiddenProblem(string detail)
    {
        return Problem(title: "Forbidden", detail: detail, statusCode: StatusCodes.Status403Forbidden);
    }
}

public sealed record CreateKnowledgeDocumentRequest(
    string Title,
    string SourceFile,
    string SourceType,
    string Content);