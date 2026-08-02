using Application.Common.Models;
using Application.Tickets.Models;
using MediatR;

namespace Application.Tickets.Commands.CreateTicketComment;

public sealed record CreateTicketCommentCommand(Guid TicketId, string Content) : IRequest<Result<TicketCommentDto>>;
