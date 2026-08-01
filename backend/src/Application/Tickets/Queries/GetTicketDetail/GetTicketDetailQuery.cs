using Application.Common.Models;
using Application.Tickets.Models;
using MediatR;

namespace Application.Tickets.Queries.GetTicketDetail;

public sealed record GetTicketDetailQuery(Guid TicketId) : IRequest<Result<TicketDetailDto>>;
