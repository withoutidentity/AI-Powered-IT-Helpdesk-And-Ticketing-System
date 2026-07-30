using Application.Chat.Models;
using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Chat.Queries.GetMessages;

public sealed class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, Result<IReadOnlyList<MessageDto>>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;

    public GetMessagesQueryHandler(ICurrentUserService currentUser, IConversationRepository conversations, IMessageRepository messages)
    {
        _currentUser = currentUser;
        _conversations = conversations;
        _messages = messages;
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversations.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return Result<IReadOnlyList<MessageDto>>.Failure("ConversationNotFound", "Conversation was not found.");
        }

        if (conversation.UserId != _currentUser.UserId)
        {
            return Result<IReadOnlyList<MessageDto>>.Failure("Forbidden", "You do not have access to this conversation.");
        }

        var messages = await _messages.ListByConversationAsync(conversation.Id, cancellationToken);
        var response = messages.Select(ToDto).ToList();

        return Result<IReadOnlyList<MessageDto>>.Success(response);
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