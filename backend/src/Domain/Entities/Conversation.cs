using Domain.Enums;

namespace Domain.Entities;

public sealed class Conversation
{
    private Conversation()
    {
        Title = string.Empty;
    }

    private Conversation(Guid id, Guid userId, string title, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Title = title;
        CreatedAt = createdAt;
        LastMessageAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastMessageAt { get; private set; }

    public static Conversation Start(Guid userId, string? title, DateTimeOffset createdAt)
    {
        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? "New conversation" : title.Trim();
        return new Conversation(Guid.NewGuid(), userId, normalizedTitle, createdAt);
    }

    public Message AddMessage(MessageSender sender, string content, MessageIntent? intent, DateTimeOffset createdAt)
    {
        var message = Message.Create(Id, sender, content, intent, createdAt);
        LastMessageAt = createdAt;
        return message;
    }
}