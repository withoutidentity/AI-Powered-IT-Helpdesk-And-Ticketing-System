using Application.Common.Models;
using Application.Tickets.Models;
using MediatR;

namespace Application.Tickets.Queries.GetTicketComments;

public sealed record GetTicketCommentsQuery(Guid TicketId) : IRequest<Result<IReadOnlyList<TicketCommentDto>>>;
