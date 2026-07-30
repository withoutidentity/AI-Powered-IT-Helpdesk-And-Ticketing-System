using Application.Chat.Models;
using Application.Common.Models;
using MediatR;

namespace Application.Chat.Queries.GetConversations;

public sealed record GetConversationsQuery : IRequest<Result<IReadOnlyList<ConversationDto>>>;