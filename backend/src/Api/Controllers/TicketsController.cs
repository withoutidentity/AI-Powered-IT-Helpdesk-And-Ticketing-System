using Application.Tickets.Commands.UpdateTicketStatus;
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

public sealed record UpdateTicketStatusRequest(string Status);
