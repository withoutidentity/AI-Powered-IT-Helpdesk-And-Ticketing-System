using Application.Chat.Models;
using Application.Common.Models;
using MediatR;

namespace Application.Chat.Commands.CreateTicketFromMessage;

public sealed record CreateTicketFromMessageCommand(
    Guid ConversationId,
    Guid MessageId,
    string? Title,
    string? Description,
    string? Priority) : IRequest<Result<TicketDto>>;