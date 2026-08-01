namespace Application.Chat.Models;

public sealed record TicketDto(
    Guid Id,
    Guid ConversationId,
    Guid? MessageId,
    Guid CreatedBy,
    Guid? AssignedTo,
    string Title,
    string Description,
    string Status,
    string Priority,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string AttachmentsJson);