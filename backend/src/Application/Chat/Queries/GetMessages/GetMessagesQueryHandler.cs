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
    private readonly IMessageSourceRepository _messageSources;

    public GetMessagesQueryHandler(
        ICurrentUserService currentUser,
        IConversationRepository conversations,
        IMessageRepository messages,
        IMessageSourceRepository messageSources)
    {
        _currentUser = currentUser;
        _conversations = conversations;
        _messages = messages;
        _messageSources = messageSources;
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
        var sourceReferences = await _messageSources.ListByMessageIdsAsync(messages.Select(message => message.Id).ToArray(), cancellationToken);
        var sourceLabelsByMessageId = sourceReferences
            .GroupBy(source => source.MessageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(ToSourceLabel).ToList());

        var response = messages
            .Select(message => ToDto(message, sourceLabelsByMessageId.GetValueOrDefault(message.Id, [])))
            .ToList();

        return Result<IReadOnlyList<MessageDto>>.Success(response);
    }

    private static string ToSourceLabel(MessageSourceReference source)
    {
        return $"{source.DocumentTitle}#chunk-{source.ChunkIndex}";
    }

    private static MessageDto ToDto(Domain.Entities.Message message, IReadOnlyList<string> sourceDocuments)
    {
        return new MessageDto(
            message.Id,
            message.ConversationId,
            message.Sender.ToString(),
            message.Content,
            message.Intent?.ToString(),
            message.CreatedAt,
            sourceDocuments);
    }
}