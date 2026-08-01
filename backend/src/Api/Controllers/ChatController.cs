using Application.Chat.Commands.CreateTicketFromMessage;
using Application.Chat.Commands.SendMessage;
using Application.Chat.Commands.StartConversation;
using Application.Chat.Queries.GetConversations;
using Application.Chat.Queries.GetMessages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly ISender _sender;

    public ChatController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> StartConversation(StartConversationRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new StartConversationCommand(request.Title), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequestProblem(result.ErrorMessage!);
        }

        return Created($"/api/v1/chat/conversations/{result.Value!.Id}", result.Value);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetConversationsQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid conversationId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMessagesQuery(conversationId), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "ConversationNotFound" => NotFoundProblem(result.ErrorMessage!),
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
    }

    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SendMessageCommand(conversationId, request.Content), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "ConversationNotFound" => NotFoundProblem(result.ErrorMessage!),
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
    }

    [HttpPost("conversations/{conversationId:guid}/messages/{messageId:guid}/ticket")]
    public async Task<IActionResult> CreateTicketFromMessage(
        Guid conversationId,
        Guid messageId,
        CreateTicketFromMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateTicketFromMessageCommand(conversationId, messageId, request.Title, request.Description, request.Priority),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "ConversationNotFound" => NotFoundProblem(result.ErrorMessage!),
                "MessageNotFound" => NotFoundProblem(result.ErrorMessage!),
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                "TicketAlreadyExists" => ConflictProblem(result.ErrorMessage!),
                "InvalidMessage" => BadRequestProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Created($"/api/v1/tickets/{result.Value!.Id}", result.Value);
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

    private IActionResult ConflictProblem(string detail)
    {
        return Problem(title: "Conflict", detail: detail, statusCode: StatusCodes.Status409Conflict);
    }

    private IActionResult ForbiddenProblem(string detail)
    {
        return Problem(title: "Forbidden", detail: detail, statusCode: StatusCodes.Status403Forbidden);
    }
}

public sealed record StartConversationRequest(string? Title);

public sealed record SendMessageRequest(string Content);

public sealed record CreateTicketFromMessageRequest(string? Title, string? Description, string? Priority);