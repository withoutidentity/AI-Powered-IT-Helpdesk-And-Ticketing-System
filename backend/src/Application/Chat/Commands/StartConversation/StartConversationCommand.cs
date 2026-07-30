using Application.Chat.Models;
using Application.Common.Models;
using MediatR;

namespace Application.Chat.Commands.StartConversation;

public sealed record StartConversationCommand(string? Title) : IRequest<Result<ConversationDto>>;