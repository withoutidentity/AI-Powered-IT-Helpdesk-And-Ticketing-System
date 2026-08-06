using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Application.UnitTests.Domain;

public sealed class CoreDataModelTests
{
    [Fact]
    public void Start_NoTitle_UsesDefaultTitleAndInitialLastMessageAt()
    {
        var userId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var conversation = Conversation.Start(userId, null, createdAt);

        conversation.UserId.Should().Be(userId);
        conversation.Title.Should().Be("New conversation");
        conversation.CreatedAt.Should().Be(createdAt);
        conversation.LastMessageAt.Should().Be(createdAt);
    }

    [Fact]
    public void AddMessage_UserMessage_UpdatesConversationLastMessageAt()
    {
        var conversation = Conversation.Start(Guid.NewGuid(), " Printer issue ", DateTimeOffset.UtcNow.AddMinutes(-5));
        var messageAt = DateTimeOffset.UtcNow;

        var message = conversation.AddMessage(MessageSender.User, " The printer is jammed. ", MessageIntent.Action, messageAt);

        conversation.Title.Should().Be("Printer issue");
        conversation.LastMessageAt.Should().Be(messageAt);
        message.ConversationId.Should().Be(conversation.Id);
        message.Sender.Should().Be(MessageSender.User);
        message.Intent.Should().Be(MessageIntent.Action);
        message.Content.Should().Be("The printer is jammed.");
    }

    [Fact]
    public void Create_NewTicket_StartsOpenWithMediumPriority()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var ticket = Ticket.Create(conversationId, messageId, createdBy, " Printer jam ", " Floor 3 printer is jammed. ", createdAt);

        ticket.ConversationId.Should().Be(conversationId);
        ticket.MessageId.Should().Be(messageId);
        ticket.CreatedBy.Should().Be(createdBy);
        ticket.Title.Should().Be("Printer jam");
        ticket.Description.Should().Be("Floor 3 printer is jammed.");
        ticket.Status.Should().Be(TicketStatus.Open);
        ticket.Priority.Should().Be(TicketPriority.Medium);
        ticket.CreatedAt.Should().Be(createdAt);
        ticket.UpdatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void UpdateStatus_ValidLifecycleTransition_UpdatesStatusAndTimestamp()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), null, Guid.NewGuid(), "Title", "Description", DateTimeOffset.UtcNow.AddMinutes(-5));
        var updatedAt = DateTimeOffset.UtcNow;

        var updated = ticket.UpdateStatus(TicketStatus.InProgress, updatedAt);

        updated.Should().BeTrue();
        ticket.Status.Should().Be(TicketStatus.InProgress);
        ticket.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void UpdateStatus_InvalidLifecycleTransition_DoesNotUpdateStatus()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var ticket = Ticket.Create(Guid.NewGuid(), null, Guid.NewGuid(), "Title", "Description", createdAt);

        var updated = ticket.UpdateStatus(TicketStatus.Closed, DateTimeOffset.UtcNow);

        updated.Should().BeFalse();
        ticket.Status.Should().Be(TicketStatus.Open);
        ticket.UpdatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void AssignTo_User_UpdatesAssigneeAndTimestamp()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), null, Guid.NewGuid(), "Title", "Description", DateTimeOffset.UtcNow.AddMinutes(-5));
        var assigneeId = Guid.NewGuid();
        var updatedAt = DateTimeOffset.UtcNow;

        ticket.AssignTo(assigneeId, updatedAt);

        ticket.AssignedTo.Should().Be(assigneeId);
        ticket.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Create_TicketComment_TrimsContent()
    {
        var ticketId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var comment = TicketComment.Create(ticketId, authorId, " Replaced toner. ", createdAt);

        comment.TicketId.Should().Be(ticketId);
        comment.AuthorId.Should().Be(authorId);
        comment.Content.Should().Be("Replaced toner.");
        comment.CreatedAt.Should().Be(createdAt);
    }
    [Fact]
    public void Create_KnowledgeDocument_StartsProcessingAndTrimsFields()
    {
        var uploadedAt = DateTimeOffset.UtcNow;

        var document = KnowledgeDocument.Create(" Wi-Fi Guide ", " wifi-guide.pdf ", " application/pdf ", " abc123 ", uploadedAt);

        document.Title.Should().Be("Wi-Fi Guide");
        document.SourceFile.Should().Be("wifi-guide.pdf");
        document.SourceType.Should().Be("application/pdf");
        document.ContentHash.Should().Be("abc123");
        document.Status.Should().Be(KnowledgeDocumentStatus.Processing);
        document.UploadedAt.Should().Be(uploadedAt);
        document.UpdatedAt.Should().Be(uploadedAt);
        document.FailureReason.Should().BeNull();
    }

    [Fact]
    public void MarkReady_FailedDocument_ClearsFailureAndUpdatesTimestamp()
    {
        var document = KnowledgeDocument.Create("VPN", "vpn.md", "text/markdown", "hash", DateTimeOffset.UtcNow.AddMinutes(-5));
        document.MarkFailed("Extraction failed", DateTimeOffset.UtcNow.AddMinutes(-3));
        var readyAt = DateTimeOffset.UtcNow;

        document.MarkReady(readyAt);

        document.Status.Should().Be(KnowledgeDocumentStatus.Ready);
        document.FailureReason.Should().BeNull();
        document.UpdatedAt.Should().Be(readyAt);
    }

    [Fact]
    public void Create_DocumentChunk_TrimsContentAndStoresEmbeddingMetadata()
    {
        var documentId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var chunk = DocumentChunk.Create(documentId, 1, " Connect to Office Wi-Fi. ", " hash-1 ", createdAt, 8, " nomic ", 768);

        chunk.DocumentId.Should().Be(documentId);
        chunk.ChunkIndex.Should().Be(1);
        chunk.Content.Should().Be("Connect to Office Wi-Fi.");
        chunk.ContentHash.Should().Be("hash-1");
        chunk.TokenCount.Should().Be(8);
        chunk.EmbeddingModel.Should().Be("nomic");
        chunk.EmbeddingDimensions.Should().Be(768);
        chunk.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Create_DocumentChunk_NegativeChunkIndex_Throws()
    {
        var act = () => DocumentChunk.Create(Guid.NewGuid(), -1, "content", "hash", DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
