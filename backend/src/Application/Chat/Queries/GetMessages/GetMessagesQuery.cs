using Application.Chat.Models;
using Application.Common.Models;
using MediatR;

namespace Application.Chat.Queries.GetMessages;

public sealed record GetMessagesQuery(Guid ConversationId) : IRequest<Result<IReadOnlyList<MessageDto>>>;