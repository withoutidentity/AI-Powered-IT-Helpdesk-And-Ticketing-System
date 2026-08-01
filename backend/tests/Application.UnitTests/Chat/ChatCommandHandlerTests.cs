using Application.Chat.Commands.CreateTicketFromMessage;
using Application.Chat.Commands.SendMessage;
using Application.Chat.Commands.StartConversation;
using Application.Chat.Queries.GetConversations;
using Application.Chat.Queries.GetMessages;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Application.UnitTests.Chat;

public sealed class ChatCommandHandlerTests
{
    [Fact]
    public async Task StartConversation_AuthenticatedUser_CreatesConversationForCurrentUser()
    {
        var currentUserId = Guid.NewGuid();
        var conversations = new FakeConversationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new StartConversationCommandHandler(new FakeCurrentUserService(currentUserId), conversations, unitOfWork);

        var result = await handler.Handle(new StartConversationCommand(" Printer help "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(currentUserId);
        result.Value.Title.Should().Be("Printer help");
        conversations.Items.Should().ContainSingle();
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetConversations_AuthenticatedUser_ReturnsOnlyCurrentUsersConversations()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(Conversation.Start(currentUserId, "Mine", DateTimeOffset.UtcNow));
        conversations.Items.Add(Conversation.Start(otherUserId, "Other", DateTimeOffset.UtcNow));
        var handler = new GetConversationsQueryHandler(new FakeCurrentUserService(currentUserId), conversations);

        var result = await handler.Handle(new GetConversationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].Title.Should().Be("Mine");
    }

    [Fact]
    public async Task SendMessage_OwnConversation_PersistsUserAndAssistantMessages()
    {
        var currentUserId = Guid.NewGuid();
        var conversation = Conversation.Start(currentUserId, "Help", DateTimeOffset.UtcNow.AddMinutes(-1));
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SendMessageCommandHandler(new FakeCurrentUserService(currentUserId), conversations, messages, unitOfWork);

        var result = await handler.Handle(new SendMessageCommand(conversation.Id, " My keyboard is broken. "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        messages.Items.Should().HaveCount(2);
        messages.Items[0].Sender.Should().Be(MessageSender.User);
        messages.Items[0].Content.Should().Be("My keyboard is broken.");
        messages.Items[1].Sender.Should().Be(MessageSender.Assistant);
        result.Value!.AssistantMessage.Content.Should().Contain("AI classification");
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task SendMessage_OtherUsersConversation_ReturnsForbidden()
    {
        var conversation = Conversation.Start(Guid.NewGuid(), "Other", DateTimeOffset.UtcNow);
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var handler = new SendMessageCommandHandler(
            new FakeCurrentUserService(Guid.NewGuid()),
            conversations,
            new FakeMessageRepository(),
            new FakeUnitOfWork());

        var result = await handler.Handle(new SendMessageCommand(conversation.Id, "Help"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
    }

    [Fact]
    public async Task GetMessages_OwnConversation_ReturnsConversationMessagesInOrder()
    {
        var currentUserId = Guid.NewGuid();
        var conversation = Conversation.Start(currentUserId, "Help", DateTimeOffset.UtcNow.AddMinutes(-2));
        var first = conversation.AddMessage(MessageSender.User, "First", null, DateTimeOffset.UtcNow.AddMinutes(-1));
        var second = conversation.AddMessage(MessageSender.Assistant, "Second", null, DateTimeOffset.UtcNow);
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var messages = new FakeMessageRepository();
        messages.Items.Add(second);
        messages.Items.Add(first);
        var handler = new GetMessagesQueryHandler(new FakeCurrentUserService(currentUserId), conversations, messages);

        var result = await handler.Handle(new GetMessagesQuery(conversation.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Content.Should().Be("First");
        result.Value[1].Content.Should().Be("Second");
    }


    [Fact]
    public async Task CreateTicketFromMessage_UserMessage_CreatesOpenTicketLinkedToMessage()
    {
        var currentUserId = Guid.NewGuid();
        var conversation = Conversation.Start(currentUserId, "Printer", DateTimeOffset.UtcNow.AddMinutes(-2));
        var message = conversation.AddMessage(MessageSender.User, " The printer is jammed on floor 3. ", null, DateTimeOffset.UtcNow.AddMinutes(-1));
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var messages = new FakeMessageRepository();
        messages.Items.Add(message);
        var tickets = new FakeTicketRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateTicketFromMessageCommandHandler(
            new FakeCurrentUserService(currentUserId),
            conversations,
            messages,
            tickets,
            unitOfWork);

        var result = await handler.Handle(
            new CreateTicketFromMessageCommand(conversation.Id, message.Id, " Printer jam ", null, "High"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tickets.Items.Should().ContainSingle();
        tickets.Items[0].MessageId.Should().Be(message.Id);
        tickets.Items[0].ConversationId.Should().Be(conversation.Id);
        tickets.Items[0].CreatedBy.Should().Be(currentUserId);
        tickets.Items[0].Title.Should().Be("Printer jam");
        tickets.Items[0].Description.Should().Be("The printer is jammed on floor 3.");
        tickets.Items[0].Priority.Should().Be(TicketPriority.High);
        result.Value!.Status.Should().Be("Open");
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task CreateTicketFromMessage_OtherUsersConversation_ReturnsForbidden()
    {
        var conversation = Conversation.Start(Guid.NewGuid(), "Other", DateTimeOffset.UtcNow);
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var handler = new CreateTicketFromMessageCommandHandler(
            new FakeCurrentUserService(Guid.NewGuid()),
            conversations,
            new FakeMessageRepository(),
            new FakeTicketRepository(),
            new FakeUnitOfWork());

        var result = await handler.Handle(new CreateTicketFromMessageCommand(conversation.Id, Guid.NewGuid(), null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
    }

    [Fact]
    public async Task CreateTicketFromMessage_AssistantMessage_ReturnsInvalidMessage()
    {
        var currentUserId = Guid.NewGuid();
        var conversation = Conversation.Start(currentUserId, "Help", DateTimeOffset.UtcNow.AddMinutes(-2));
        var message = conversation.AddMessage(MessageSender.Assistant, "Assistant response", null, DateTimeOffset.UtcNow.AddMinutes(-1));
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var messages = new FakeMessageRepository();
        messages.Items.Add(message);
        var handler = new CreateTicketFromMessageCommandHandler(
            new FakeCurrentUserService(currentUserId),
            conversations,
            messages,
            new FakeTicketRepository(),
            new FakeUnitOfWork());

        var result = await handler.Handle(new CreateTicketFromMessageCommand(conversation.Id, message.Id, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidMessage");
    }

    [Fact]
    public async Task CreateTicketFromMessage_ExistingTicketForMessage_ReturnsConflict()
    {
        var currentUserId = Guid.NewGuid();
        var conversation = Conversation.Start(currentUserId, "Help", DateTimeOffset.UtcNow.AddMinutes(-2));
        var message = conversation.AddMessage(MessageSender.User, "Need help", null, DateTimeOffset.UtcNow.AddMinutes(-1));
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var messages = new FakeMessageRepository();
        messages.Items.Add(message);
        var tickets = new FakeTicketRepository();
        tickets.Items.Add(Ticket.Create(conversation.Id, message.Id, currentUserId, "Need help", "Need help", DateTimeOffset.UtcNow));
        var handler = new CreateTicketFromMessageCommandHandler(
            new FakeCurrentUserService(currentUserId),
            conversations,
            messages,
            tickets,
            new FakeUnitOfWork());

        var result = await handler.Handle(new CreateTicketFromMessageCommand(conversation.Id, message.Id, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TicketAlreadyExists");
    }
    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; }
    }

    private sealed class FakeConversationRepository : IConversationRepository
    {
        public List<Conversation> Items { get; } = new();

        public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(conversation => conversation.Id == id));
        }

        public Task<IReadOnlyList<Conversation>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            IReadOnlyList<Conversation> result = Items
                .Where(conversation => conversation.UserId == userId)
                .OrderByDescending(conversation => conversation.LastMessageAt)
                .ToList();

            return Task.FromResult(result);
        }

        public Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
        {
            Items.Add(conversation);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMessageRepository : IMessageRepository
    {
        public List<Message> Items { get; } = new();

        public Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(message => message.Id == id));
        }

        public Task<IReadOnlyList<Message>> ListByConversationAsync(Guid conversationId, CancellationToken cancellationToken)
        {
            IReadOnlyList<Message> result = Items
                .Where(message => message.ConversationId == conversationId)
                .OrderBy(message => message.CreatedAt)
                .ToList();

            return Task.FromResult(result);
        }

        public Task AddAsync(Message message, CancellationToken cancellationToken)
        {
            Items.Add(message);
            return Task.CompletedTask;
        }
    }


    private sealed class FakeTicketRepository : ITicketRepository
    {
        public List<Ticket> Items { get; } = new();

        public Task<Ticket?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(ticket => ticket.MessageId == messageId));
        }

        public Task AddAsync(Ticket ticket, CancellationToken cancellationToken)
        {
            Items.Add(ticket);
            return Task.CompletedTask;
        }
    }
    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }
}