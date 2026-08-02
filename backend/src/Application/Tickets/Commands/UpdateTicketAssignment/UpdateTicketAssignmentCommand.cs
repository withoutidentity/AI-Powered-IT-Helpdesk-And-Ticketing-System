using Application.Common.Models;
using Application.Tickets.Models;
using MediatR;

namespace Application.Tickets.Commands.UpdateTicketAssignment;

public sealed record UpdateTicketAssignmentCommand(Guid TicketId, Guid AssignedToUserId) : IRequest<Result<TicketDetailDto>>;
