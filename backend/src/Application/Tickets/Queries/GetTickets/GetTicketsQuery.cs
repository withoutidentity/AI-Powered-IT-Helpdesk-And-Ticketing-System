using Application.Common.Models;
using Application.Tickets.Models;
using MediatR;

namespace Application.Tickets.Queries.GetTickets;

public sealed record GetTicketsQuery(string? Status, string? Priority, int Page = 1, int PageSize = 20)
    : IRequest<Result<PaginatedList<TicketSummaryDto>>>;
