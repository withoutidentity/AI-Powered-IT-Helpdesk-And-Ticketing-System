using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Tickets.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Tickets.Queries.GetTickets;

public sealed class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, Result<PaginatedList<TicketSummaryDto>>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;

    public GetTicketsQueryHandler(ICurrentUserService currentUser, ITicketRepository tickets, IUserRepository users)
    {
        _currentUser = currentUser;
        _tickets = tickets;
        _users = users;
    }

    public async Task<Result<PaginatedList<TicketSummaryDto>>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        var criteria = BuildCriteria(request);
        var tickets = await _tickets.ListAsync(criteria, cancellationToken);
        var totalCount = await _tickets.CountAsync(criteria, cancellationToken);
        var usersById = await LoadUsersByIdAsync(tickets, cancellationToken);

        var items = tickets.Select(ticket => ToSummaryDto(ticket, usersById)).ToList();
        return Result<PaginatedList<TicketSummaryDto>>.Success(new PaginatedList<TicketSummaryDto>(items, request.Page, request.PageSize, totalCount));
    }

    private TicketListCriteria BuildCriteria(GetTicketsQuery request)
    {
        TicketStatus? status = string.IsNullOrWhiteSpace(request.Status) ? null : Enum.Parse<TicketStatus>(request.Status, ignoreCase: true);
        TicketPriority? priority = string.IsNullOrWhiteSpace(request.Priority) ? null : Enum.Parse<TicketPriority>(request.Priority, ignoreCase: true);

        return _currentUser.Role switch
        {
            UserRole.Employee => new TicketListCriteria(status, priority, CreatedBy: _currentUser.UserId, AssignedTo: null, IncludeUnassigned: false, IncludeAll: false, request.Page, request.PageSize),
            UserRole.ITAgent => new TicketListCriteria(status, priority, CreatedBy: null, AssignedTo: _currentUser.UserId, IncludeUnassigned: true, IncludeAll: false, request.Page, request.PageSize),
            UserRole.ITAdmin => new TicketListCriteria(status, priority, CreatedBy: null, AssignedTo: null, IncludeUnassigned: false, IncludeAll: true, request.Page, request.PageSize),
            _ => new TicketListCriteria(status, priority, CreatedBy: _currentUser.UserId, AssignedTo: null, IncludeUnassigned: false, IncludeAll: false, request.Page, request.PageSize)
        };
    }

    private async Task<IReadOnlyDictionary<Guid, User>> LoadUsersByIdAsync(IReadOnlyList<Ticket> tickets, CancellationToken cancellationToken)
    {
        var userIds = tickets
            .Select(ticket => ticket.CreatedBy)
            .Concat(tickets.Where(ticket => ticket.AssignedTo is not null).Select(ticket => ticket.AssignedTo!.Value))
            .Distinct()
            .ToList();

        var users = await _users.ListByIdsAsync(userIds, cancellationToken);
        return users.ToDictionary(user => user.Id);
    }

    private static TicketSummaryDto ToSummaryDto(Ticket ticket, IReadOnlyDictionary<Guid, User> usersById)
    {
        return new TicketSummaryDto(
            ticket.Id,
            ticket.ConversationId,
            ticket.MessageId,
            ticket.Title,
            ticket.Status.ToString(),
            ticket.Priority.ToString(),
            ToUserRef(ticket.CreatedBy, usersById),
            ticket.AssignedTo is null ? null : ToUserRef(ticket.AssignedTo.Value, usersById),
            ticket.CreatedAt,
            ticket.UpdatedAt);
    }

    private static UserRefDto ToUserRef(Guid userId, IReadOnlyDictionary<Guid, User> usersById)
    {
        return usersById.TryGetValue(userId, out var user)
            ? new UserRefDto(user.Id, user.Username)
            : new UserRefDto(userId, "unknown");
    }
}

