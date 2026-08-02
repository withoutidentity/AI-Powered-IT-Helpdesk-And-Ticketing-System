namespace Application.Tickets.Models;

public sealed record TicketCommentDto(
    Guid Id,
    Guid TicketId,
    UserRefDto Author,
    string Content,
    DateTimeOffset CreatedAt);
