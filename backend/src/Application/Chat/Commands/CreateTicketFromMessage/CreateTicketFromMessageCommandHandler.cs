using Application.Chat.Models;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Chat.Commands.CreateTicketFromMessage;

public sealed class CreateTicketFromMessageCommandHandler : IRequestHandler<CreateTicketFromMessageCommand, Result<TicketDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;
    private readonly ITicketRepository _tickets;
    private readonly ITicketActivityRepository _activities;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTicketFromMessageCommandHandler(
        ICurrentUserService currentUser,
        IConversationRepository conversations,
        IMessageRepository messages,
        ITicketRepository tickets,
        ITicketActivityRepository activities,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _conversations = conversations;
        _messages = messages;
        _tickets = tickets;
        _activities = activities;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TicketDto>> Handle(CreateTicketFromMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversations.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return Result<TicketDto>.Failure("ConversationNotFound", "Conversation was not found.");
        }

        if (conversation.UserId != _currentUser.UserId)
        {
            return Result<TicketDto>.Failure("Forbidden", "You do not have access to this conversation.");
        }

        var message = await _messages.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null || message.ConversationId != conversation.Id)
        {
            return Result<TicketDto>.Failure("MessageNotFound", "Message was not found in this conversation.");
        }

        if (message.Sender != MessageSender.User)
        {
            return Result<TicketDto>.Failure("InvalidMessage", "Only user messages can create tickets.");
        }

        var existingTicket = await _tickets.GetByConversationIdAsync(conversation.Id, cancellationToken);
        if (existingTicket is not null)
        {
            return Result<TicketDto>.Failure("TicketAlreadyExists", "A ticket already exists for this conversation.");
        }

        var priority = ParsePriority(request.Priority);
        var ticket = Ticket.Create(
            conversation.Id,
            message.Id,
            _currentUser.UserId,
            BuildTitle(request.Title, message.Content),
            BuildDescription(request.Description, message.Content),
            DateTimeOffset.UtcNow,
            priority);

        await _tickets.AddAsync(ticket, cancellationToken);
        await _activities.AddAsync(TicketActivity.Create(
            ticket.Id,
            _currentUser.UserId,
            "TicketCreated",
            ticket.CreatedAt,
            "status",
            null,
            ticket.Status.ToString()), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TicketDto>.Success(ToDto(ticket));
    }

    private static TicketPriority ParsePriority(string? priority)
    {
        return Enum.TryParse<TicketPriority>(priority, true, out var parsed) ? parsed : TicketPriority.Medium;
    }

    private static string BuildTitle(string? requestedTitle, string messageContent)
    {
        var title = string.IsNullOrWhiteSpace(requestedTitle) ? messageContent.Trim() : requestedTitle.Trim();
        return title.Length <= 120 ? title : $"{title[..117]}...";
    }

    private static string BuildDescription(string? requestedDescription, string messageContent)
    {
        return string.IsNullOrWhiteSpace(requestedDescription) ? messageContent.Trim() : requestedDescription.Trim();
    }

    private static TicketDto ToDto(Ticket ticket)
    {
        return new TicketDto(
            ticket.Id,
            ticket.ConversationId,
            ticket.MessageId,
            ticket.CreatedBy,
            ticket.AssignedTo,
            ticket.Title,
            ticket.Description,
            ticket.Status.ToString(),
            ticket.Priority.ToString(),
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.AttachmentsJson);
    }
}
