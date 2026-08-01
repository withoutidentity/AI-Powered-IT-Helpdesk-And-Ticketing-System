using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Tickets.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Tickets.Commands.UpdateTicketStatus;

public sealed class UpdateTicketStatusCommandHandler : IRequestHandler<UpdateTicketStatusCommand, Result<TicketDetailDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTicketStatusCommandHandler(
        ICurrentUserService currentUser,
        ITicketRepository tickets,
        IUserRepository users,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _tickets = tickets;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TicketDetailDto>> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketDetailDto>.Failure("TicketNotFound", "Ticket was not found.");
        }

        if (!CanUpdate(ticket))
        {
            return Result<TicketDetailDto>.Failure("Forbidden", "You do not have permission to update this ticket.");
        }

        var targetStatus = Enum.Parse<TicketStatus>(request.Status, ignoreCase: true);
        if (!ticket.UpdateStatus(targetStatus, DateTimeOffset.UtcNow))
        {
            return Result<TicketDetailDto>.Failure("InvalidStatusTransition", $"Cannot transition ticket from {ticket.Status} to {targetStatus}.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var usersById = await LoadUsersByIdAsync(ticket, cancellationToken);
        return Result<TicketDetailDto>.Success(ToDetailDto(ticket, usersById));
    }

    private bool CanUpdate(Ticket ticket)
    {
        return _currentUser.Role switch
        {
            UserRole.ITAdmin => true,
            UserRole.ITAgent => ticket.AssignedTo is null || ticket.AssignedTo == _currentUser.UserId,
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
