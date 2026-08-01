using Application.Common.Models;
using Application.Tickets.Models;
using MediatR;

namespace Application.Tickets.Commands.UpdateTicketStatus;

public sealed record UpdateTicketStatusCommand(Guid TicketId, string Status) : IRequest<Result<TicketDetailDto>>;
