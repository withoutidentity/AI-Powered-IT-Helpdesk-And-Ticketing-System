namespace Domain.Entities;

public sealed class TicketActivity
{
    private TicketActivity()
    {
        Action = string.Empty;
    }

    private TicketActivity(
        Guid id,
        Guid ticketId,
        Guid actorId,
        string action,
        string? field,
        string? oldValue,
        string? newValue,
        DateTimeOffset createdAt)
    {
        Id = id;
        TicketId = ticketId;
        ActorId = actorId;
        Action = action;
        Field = field;
        OldValue = oldValue;
        NewValue = newValue;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid ActorId { get; private set; }
    public string Action { get; private set; }
    public string? Field { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static TicketActivity Create(
        Guid ticketId,
        Guid actorId,
        string action,
        DateTimeOffset createdAt,
        string? field = null,
        string? oldValue = null,
        string? newValue = null)
    {
        return new TicketActivity(
            Guid.NewGuid(),
            ticketId,
            actorId,
            action.Trim(),
            string.IsNullOrWhiteSpace(field) ? null : field.Trim(),
            string.IsNullOrWhiteSpace(oldValue) ? null : oldValue.Trim(),
            string.IsNullOrWhiteSpace(newValue) ? null : newValue.Trim(),
            createdAt);
    }
}
