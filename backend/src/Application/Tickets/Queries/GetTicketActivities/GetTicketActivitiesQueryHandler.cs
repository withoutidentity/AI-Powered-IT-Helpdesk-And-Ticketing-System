using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Tickets.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Tickets.Queries.GetTicketActivities;

public sealed class GetTicketActivitiesQueryHandler : IRequestHandler<GetTicketActivitiesQuery, Result<IReadOnlyList<TicketActivityDto>>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITicketRepository _tickets;
    private readonly ITicketActivityRepository _activities;
    private readonly IUserRepository _users;

    public GetTicketActivitiesQueryHandler(ICurrentUserService currentUser, ITicketRepository tickets, ITicketActivityRepository activities, IUserRepository users)
    {
        _currentUser = currentUser;
        _tickets = tickets;
        _activities = activities;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<TicketActivityDto>>> Handle(GetTicketActivitiesQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<IReadOnlyList<TicketActivityDto>>.Failure("TicketNotFound", "Ticket was not found.");
        }

        if (!CanAccess(ticket))
        {
            return Result<IReadOnlyList<TicketActivityDto>>.Failure("Forbidden", "You do not have access to this ticket.");
        }

        var activities = await _activities.ListByTicketIdAsync(ticket.Id, cancellationToken);
        var usersById = await LoadUsersByIdAsync(activities.Select(activity => activity.ActorId), cancellationToken);
        var result = activities.Select(activity => ToDto(activity, usersById)).ToList();

        return Result<IReadOnlyList<TicketActivityDto>>.Success(result);
    }

    private bool CanAccess(Ticket ticket)
    {
        return _currentUser.Role switch
        {
            UserRole.Employee => ticket.CreatedBy == _currentUser.UserId,
            UserRole.ITAgent => ticket.AssignedTo is null || ticket.AssignedTo == _currentUser.UserId,
            UserRole.ITAdmin => true,
            _ => false
        };
    }

    private async Task<IReadOnlyDictionary<Guid, User>> LoadUsersByIdAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var distinctIds = ids.Distinct().ToList();
        var users = await _users.ListByIdsAsync(distinctIds, cancellationToken);
        return users.ToDictionary(user => user.Id);
    }

    private static TicketActivityDto ToDto(TicketActivity activity, IReadOnlyDictionary<Guid, User> usersById)
    {
        return new TicketActivityDto(
            activity.Id,
            activity.TicketId,
            ToUserRef(activity.ActorId, usersById),
            activity.Action,
            activity.Field,
            activity.OldValue,
            activity.NewValue,
            activity.CreatedAt);
    }

    private static UserRefDto ToUserRef(Guid userId, IReadOnlyDictionary<Guid, User> usersById)
    {
        return usersById.TryGetValue(userId, out var user)
            ? new UserRefDto(user.Id, user.Username)
            : new UserRefDto(userId, "unknown");
    }
}
