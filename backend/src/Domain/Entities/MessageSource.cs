namespace Domain.Entities;

public sealed class MessageSource
{
    private MessageSource()
    {
    }

    private MessageSource(Guid id, Guid messageId, Guid documentChunkId, DateTimeOffset createdAt)
    {
        Id = id;
        MessageId = messageId;
        DocumentChunkId = documentChunkId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public Guid DocumentChunkId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static MessageSource Create(Guid messageId, Guid documentChunkId, DateTimeOffset createdAt)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        if (documentChunkId == Guid.Empty)
        {
            throw new ArgumentException("Document chunk id is required.", nameof(documentChunkId));
        }

        return new MessageSource(Guid.NewGuid(), messageId, documentChunkId, createdAt);
    }
}