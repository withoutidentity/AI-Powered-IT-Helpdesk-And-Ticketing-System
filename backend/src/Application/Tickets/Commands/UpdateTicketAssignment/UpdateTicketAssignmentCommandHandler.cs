using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Tickets.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Tickets.Commands.UpdateTicketAssignment;

public sealed class UpdateTicketAssignmentCommandHandler : IRequestHandler<UpdateTicketAssignmentCommand, Result<TicketDetailDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly ITicketActivityRepository _activities;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTicketAssignmentCommandHandler(
        ICurrentUserService currentUser,
        ITicketRepository tickets,
        IUserRepository users,
        ITicketActivityRepository activities,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _tickets = tickets;
        _users = users;
        _activities = activities;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TicketDetailDto>> Handle(UpdateTicketAssignmentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketDetailDto>.Failure("TicketNotFound", "Ticket was not found.");
        }

        if (!CanAssign(ticket, request.AssignedToUserId))
        {
            return Result<TicketDetailDto>.Failure("Forbidden", "You do not have permission to assign this ticket.");
        }

        var assignee = await _users.GetByIdAsync(request.AssignedToUserId, cancellationToken);
        if (assignee is null)
        {
            return Result<TicketDetailDto>.Failure("UserNotFound", "Assigned user was not found.");
        }

        if (assignee.Role != UserRole.ITAgent)
        {
            return Result<TicketDetailDto>.Failure("InvalidAssignee", "Tickets can only be assigned to IT agents.");
        }

        var oldAssignee = ticket.AssignedTo;
        var oldAssigneeUser = oldAssignee is null ? null : await _users.GetByIdAsync(oldAssignee.Value, cancellationToken);
        var updatedAt = DateTimeOffset.UtcNow;
        ticket.AssignTo(assignee.Id, updatedAt);
        await _activities.AddAsync(TicketActivity.Create(
            ticket.Id,
            _currentUser.UserId,
            "Assigned",
            updatedAt,
            "assignedTo",
            oldAssigneeUser?.Username,
            assignee.Username), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var usersById = await LoadUsersByIdAsync(ticket, cancellationToken);
        return Result<TicketDetailDto>.Success(ToDetailDto(ticket, usersById));
    }

    private bool CanAssign(Ticket ticket, Guid assignedToUserId)
    {
        return _currentUser.Role switch
        {
            UserRole.ITAdmin => true,
            UserRole.ITAgent => assignedToUserId == _currentUser.UserId
                && (ticket.AssignedTo is null || ticket.AssignedTo == _currentUser.UserId),
            _ => false
        };
    }

    private async Task<IReadOnlyDictionary<Guid, User>> LoadUsersByIdAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        var ids = new List<Guid> { ticket.CreatedBy };
        if (ticket.AssignedTo is not null)
        {
            ids.Add(ticket.AssignedTo.Value);
        }

        var users = await _users.ListByIdsAsync(ids.Distinct().ToList(), cancellationToken);
        return users.ToDictionary(user => user.Id);
    }

    private static TicketDetailDto ToDetailDto(Ticket ticket, IReadOnlyDictionary<Guid, User> usersById)
    {
        return new TicketDetailDto(
            ticket.Id,
            ticket.ConversationId,
            ticket.MessageId,
            ticket.Title,
            ticket.Description,
            ticket.Status.ToString(),
            ticket.Priority.ToString(),
            ToUserRef(ticket.CreatedBy, usersById),
            ticket.AssignedTo is null ? null : ToUserRef(ticket.AssignedTo.Value, usersById),
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.AttachmentsJson);
    }

    private static UserRefDto ToUserRef(Guid userId, IReadOnlyDictionary<Guid, User> usersById)
    {
        return usersById.TryGetValue(userId, out var user)
            ? new UserRefDto(user.Id, user.Username)
            : new UserRefDto(userId, "unknown");
    }
}
