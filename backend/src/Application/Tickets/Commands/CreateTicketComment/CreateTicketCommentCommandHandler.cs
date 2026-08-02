using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Tickets.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Tickets.Commands.CreateTicketComment;

public sealed class CreateTicketCommentCommandHandler : IRequestHandler<CreateTicketCommentCommand, Result<TicketCommentDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITicketRepository _tickets;
    private readonly ITicketCommentRepository _comments;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTicketCommentCommandHandler(
        ICurrentUserService currentUser,
        ITicketRepository tickets,
        ITicketCommentRepository comments,
        IUserRepository users,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _tickets = tickets;
        _comments = comments;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TicketCommentDto>> Handle(CreateTicketCommentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketCommentDto>.Failure("TicketNotFound", "Ticket was not found.");
        }

        if (!CanAccess(ticket))
        {
            return Result<TicketCommentDto>.Failure("Forbidden", "You do not have access to this ticket.");
        }

        var comment = TicketComment.Create(ticket.Id, _currentUser.UserId, request.Content, DateTimeOffset.UtcNow);
        await _comments.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var author = await _users.GetByIdAsync(comment.AuthorId, cancellationToken);
        return Result<TicketCommentDto>.Success(ToDto(comment, author));
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

    private static TicketCommentDto ToDto(TicketComment comment, User? author)
    {
        return new TicketCommentDto(
            comment.Id,
            comment.TicketId,
            author is null ? new UserRefDto(comment.AuthorId, "unknown") : new UserRefDto(author.Id, author.Username),
            comment.Content,
            comment.CreatedAt);
    }
}
