namespace Application.Tickets.Models;

public sealed record TicketSummaryDto(
    Guid Id,
    Guid ConversationId,
    Guid? MessageId,
    string Title,
    string Status,
    string Priority,
    UserRefDto CreatedBy,
    UserRefDto? AssignedTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
