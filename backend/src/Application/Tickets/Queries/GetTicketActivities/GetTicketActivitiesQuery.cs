using Application.Common.Models;
using Application.Tickets.Models;
using MediatR;

namespace Application.Tickets.Queries.GetTicketActivities;

public sealed record GetTicketActivitiesQuery(Guid TicketId) : IRequest<Result<IReadOnlyList<TicketActivityDto>>>;
