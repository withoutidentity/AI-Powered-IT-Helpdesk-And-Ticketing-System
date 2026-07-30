using Application.Chat.Models;
using Application.Common.Models;
using MediatR;

namespace Application.Chat.Commands.SendMessage;

public sealed record SendMessageCommand(Guid ConversationId, string Content) : IRequest<Result<SendMessageResponse>>;