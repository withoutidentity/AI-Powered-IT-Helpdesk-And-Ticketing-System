using Application.Tickets.Commands.CreateTicketComment;
using Application.Tickets.Commands.UpdateTicketAssignment;
using Application.Tickets.Commands.UpdateTicketStatus;
using Application.Tickets.Queries.GetTicketActivities;
using Application.Tickets.Queries.GetTicketComments;
using Application.Tickets.Queries.GetTicketDetail;
using Application.Tickets.Queries.GetTickets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly ISender _sender;

    public TicketsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetTicketsQuery(status, priority, page, pageSize), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<IActionResult> GetTicketDetail(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTicketDetailQuery(ticketId), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "TicketNotFound" => NotFoundProblem(result.ErrorMessage!),
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
    }

    [HttpPatch("{ticketId:guid}/status")]
    public async Task<IActionResult> UpdateTicketStatus(Guid ticketId, UpdateTicketStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateTicketStatusCommand(ticketId, request.Status), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "TicketNotFound" => NotFoundProblem(result.ErrorMessage!),
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                "InvalidStatusTransition" => ConflictProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
    }

    [HttpPatch("{ticketId:guid}/assignment")]
    public async Task<IActionResult> UpdateTicketAssignment(Guid ticketId, UpdateTicketAssignmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateTicketAssignmentCommand(ticketId, request.AssignedToUserId), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "TicketNotFound" or "UserNotFound" => NotFoundProblem(result.ErrorMessage!),
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                "InvalidAssignee" or "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
    }

    [HttpGet("{ticketId:guid}/comments")]
    public async Task<IActionResult> GetTicketComments(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTicketCommentsQuery(ticketId), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "TicketNotFound" => NotFoundProblem(result.ErrorMessage!),
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
    }

    [HttpPost("{ticketId:guid}/comments")]
    public async Task<IActionResult> CreateTicketComment(Guid ticketId, CreateTicketCommentRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateTicketCommentCommand(ticketId, request.Content), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "TicketNotFound" => NotFoundProblem(result.ErrorMessage!),
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return CreatedAtAction(nameof(GetTicketComments), new { ticketId }, result.Value);
    }

    [HttpGet("{ticketId:guid}/activities")]
    public async Task<IActionResult> GetTicketActivities(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTicketActivitiesQuery(ticketId), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "TicketNotFound" => NotFoundProblem(result.ErrorMessage!),
                "Forbidden" => ForbiddenProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
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

    private IActionResult ConflictProblem(string detail)
    {
        return Problem(title: "Conflict", detail: detail, statusCode: StatusCodes.Status409Conflict);
    }
}

public sealed record CreateTicketCommentRequest(string Content);

public sealed record UpdateTicketStatusRequest(string Status);

public sealed record UpdateTicketAssignmentRequest(Guid AssignedToUserId);
