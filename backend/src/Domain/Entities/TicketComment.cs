namespace Domain.Entities;

public sealed class TicketComment
{
    private TicketComment()
    {
        Content = string.Empty;
    }

    private TicketComment(Guid id, Guid ticketId, Guid authorId, string content, DateTimeOffset createdAt)
    {
        Id = id;
        TicketId = ticketId;
        AuthorId = authorId;
        Content = content;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static TicketComment Create(Guid ticketId, Guid authorId, string content, DateTimeOffset createdAt)
    {
        return new TicketComment(Guid.NewGuid(), ticketId, authorId, content.Trim(), createdAt);
    }
}