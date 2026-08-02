namespace Application.Tickets.Models;

public sealed record TicketActivityDto(
    Guid Id,
    Guid TicketId,
    UserRefDto Actor,
    string Action,
    string? Field,
    string? OldValue,
    string? NewValue,
    DateTimeOffset CreatedAt);
