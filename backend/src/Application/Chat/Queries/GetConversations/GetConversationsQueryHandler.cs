using Application.Chat.Models;
using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Chat.Queries.GetConversations;

public sealed class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, Result<IReadOnlyList<ConversationDto>>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IConversationRepository _conversations;

    public GetConversationsQueryHandler(ICurrentUserService currentUser, IConversationRepository conversations)
    {
        _currentUser = currentUser;
        _conversations = conversations;
    }

    public async Task<Result<IReadOnlyList<ConversationDto>>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversations = await _conversations.ListByUserAsync(_currentUser.UserId, cancellationToken);
        var response = conversations
            .Select(conversation => new ConversationDto(conversation.Id, conversation.UserId, conversation.Title, conversation.CreatedAt, conversation.LastMessageAt))
            .ToList();

        return Result<IReadOnlyList<ConversationDto>>.Success(response);
    }
}