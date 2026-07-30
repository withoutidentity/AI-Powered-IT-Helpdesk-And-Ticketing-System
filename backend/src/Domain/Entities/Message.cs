using Domain.Enums;

namespace Domain.Entities;

public sealed class Message
{
    private Message()
    {
        Content = string.Empty;
    }

    private Message(Guid id, Guid conversationId, MessageSender sender, string content, MessageIntent? intent, DateTimeOffset createdAt)
    {
        Id = id;
        ConversationId = conversationId;
        Sender = sender;
        Content = content;
        Intent = intent;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public MessageSender Sender { get; private set; }
    public string Content { get; private set; }
    public MessageIntent? Intent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Message Create(Guid conversationId, MessageSender sender, string content, MessageIntent? intent, DateTimeOffset createdAt)
    {
        return new Message(Guid.NewGuid(), conversationId, sender, content.Trim(), intent, createdAt);
    }
}