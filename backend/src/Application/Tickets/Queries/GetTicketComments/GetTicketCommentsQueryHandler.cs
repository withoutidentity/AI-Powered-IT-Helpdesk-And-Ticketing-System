using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Tickets.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Tickets.Queries.GetTicketComments;

public sealed class GetTicketCommentsQueryHandler : IRequestHandler<GetTicketCommentsQuery, Result<IReadOnlyList<TicketCommentDto>>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITicketRepository _tickets;
    private readonly ITicketCommentRepository _comments;
    private readonly IUserRepository _users;

    public GetTicketCommentsQueryHandler(ICurrentUserService currentUser, ITicketRepository tickets, ITicketCommentRepository comments, IUserRepository users)
    {
        _currentUser = currentUser;
        _tickets = tickets;
        _comments = comments;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<TicketCommentDto>>> Handle(GetTicketCommentsQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<IReadOnlyList<TicketCommentDto>>.Failure("TicketNotFound", "Ticket was not found.");
        }

        if (!CanAccess(ticket))
        {
            return Result<IReadOnlyList<TicketCommentDto>>.Failure("Forbidden", "You do not have access to this ticket.");
        }

        var comments = await _comments.ListByTicketIdAsync(ticket.Id, cancellationToken);
        var usersById = await LoadUsersByIdAsync(comments.Select(comment => comment.AuthorId), cancellationToken);
        var result = comments.Select(comment => ToDto(comment, usersById)).ToList();

        return Result<IReadOnlyList<TicketCommentDto>>.Success(result);
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

    private static TicketCommentDto ToDto(TicketComment comment, IReadOnlyDictionary<Guid, User> usersById)
    {
        return new TicketCommentDto(
            comment.Id,
            comment.TicketId,
            ToUserRef(comment.AuthorId, usersById),
            comment.Content,
            comment.CreatedAt);
    }

    private static UserRefDto ToUserRef(Guid userId, IReadOnlyDictionary<Guid, User> usersById)
    {
        return usersById.TryGetValue(userId, out var user)
            ? new UserRefDto(user.Id, user.Username)
            : new UserRefDto(userId, "unknown");
    }
}
