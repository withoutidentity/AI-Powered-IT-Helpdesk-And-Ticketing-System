using Application.Chat.Models;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;

namespace Application.Chat.Commands.SendMessage;

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<SendMessageResponse>>
{
    private const string CannedAssistantResponse = "I recorded your message. AI classification and RAG answers will be enabled in a later slice.";

    private readonly ICurrentUserService _currentUser;
    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;
    private readonly IUnitOfWork _unitOfWork;

    public SendMessageCommandHandler(
        ICurrentUserService currentUser,
        IConversationRepository conversations,
        IMessageRepository messages,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _conversations = conversations;
        _messages = messages;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SendMessageResponse>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversations.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return Result<SendMessageResponse>.Failure("ConversationNotFound", "Conversation was not found.");
        }

        if (conversation.UserId != _currentUser.UserId)
        {
            return Result<SendMessageResponse>.Failure("Forbidden", "You do not have access to this conversation.");
        }

        var now = DateTimeOffset.UtcNow;
        var userMessage = conversation.AddMessage(MessageSender.User, request.Content, null, now);
        var assistantMessage = conversation.AddMessage(MessageSender.Assistant, CannedAssistantResponse, null, now.AddMilliseconds(1));

        await _messages.AddAsync(userMessage, cancellationToken);
        await _messages.AddAsync(assistantMessage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SendMessageResponse>.Success(new SendMessageResponse(ToDto(userMessage), ToDto(assistantMessage)));
    }

    private static MessageDto ToDto(Domain.Entities.Message message)
    {
        return new MessageDto(
            message.Id,
            message.ConversationId,
            message.Sender.ToString(),
            message.Content,
            message.Intent?.ToString(),
            message.CreatedAt);
    }
}