using Application.Chat.Models;
using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Chat.Queries.GetConversations;

public sealed class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, Result<IReadOnlyList<ConversationDto>>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IConversationRepository _conversations;
    private readonly ITicketRepository _tickets;

    public GetConversationsQueryHandler(ICurrentUserService currentUser, IConversationRepository conversations, ITicketRepository tickets)
    {
        _currentUser = currentUser;
        _conversations = conversations;
        _tickets = tickets;
    }

    public async Task<Result<IReadOnlyList<ConversationDto>>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversations = await _conversations.ListByUserAsync(_currentUser.UserId, cancellationToken);
        var conversationIds = conversations.Select(conversation => conversation.Id).ToList();
        var conversationIdsWithTickets = await _tickets.ListConversationIdsWithTicketsAsync(conversationIds, cancellationToken);

        var response = conversations
            .Select(conversation => new ConversationDto(
                conversation.Id,
                conversation.UserId,
                conversation.Title,
                conversation.CreatedAt,
                conversation.LastMessageAt,
                conversationIdsWithTickets.Contains(conversation.Id)))
            .ToList();

        return Result<IReadOnlyList<ConversationDto>>.Success(response);
    }
}