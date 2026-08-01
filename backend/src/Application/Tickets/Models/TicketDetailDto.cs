namespace Application.Tickets.Models;

public sealed record TicketDetailDto(
    Guid Id,
    Guid ConversationId,
    Guid? MessageId,
    string Title,
    string Description,
    string Status,
    string Priority,
    UserRefDto CreatedBy,
    UserRefDto? AssignedTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string AttachmentsJson);
