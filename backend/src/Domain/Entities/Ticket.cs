using Domain.Enums;

namespace Domain.Entities;

public sealed class Ticket
{
    private Ticket()
    {
        Title = string.Empty;
        Description = string.Empty;
        AttachmentsJson = "[]";
    }

    private Ticket(
        Guid id,
        Guid conversationId,
        Guid? messageId,
        Guid createdBy,
        string title,
        string description,
        TicketPriority priority,
        DateTimeOffset createdAt,
        string attachmentsJson)
    {
        Id = id;
        ConversationId = conversationId;
        MessageId = messageId;
        CreatedBy = createdBy;
        Title = title;
        Description = description;
        AttachmentsJson = attachmentsJson;
        Status = TicketStatus.Open;
        Priority = priority;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid? MessageId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? AssignedTo { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string AttachmentsJson { get; private set; }
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Ticket Create(
        Guid conversationId,
        Guid? messageId,
        Guid createdBy,
        string title,
        string description,
        DateTimeOffset createdAt,
        TicketPriority priority = TicketPriority.Medium,
        string attachmentsJson = "[]")
    {
        return new Ticket(
            Guid.NewGuid(),
            conversationId,
            messageId,
            createdBy,
            title.Trim(),
            description.Trim(),
            priority,
            createdAt,
            string.IsNullOrWhiteSpace(attachmentsJson) ? "[]" : attachmentsJson.Trim());
    }

    public bool CanTransitionTo(TicketStatus target)
    {
        return Status switch
        {
            TicketStatus.Open => target == TicketStatus.InProgress,
            TicketStatus.InProgress => target == TicketStatus.Resolved,
            TicketStatus.Resolved => target is TicketStatus.Closed or TicketStatus.InProgress,
            TicketStatus.Closed => false,
            _ => false
        };
    }

    public bool UpdateStatus(TicketStatus target, DateTimeOffset updatedAt)
    {
        if (!CanTransitionTo(target))
        {
            return false;
        }

        Status = target;
        UpdatedAt = updatedAt;
        return true;
    }

    public void AssignTo(Guid? assignedTo, DateTimeOffset updatedAt)
    {
        AssignedTo = assignedTo;
        UpdatedAt = updatedAt;
    }
}