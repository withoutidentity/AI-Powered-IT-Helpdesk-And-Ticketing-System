using Application.Chat.Commands.CreateTicketFromMessage;
using Application.Chat.Commands.SendMessage;
using Application.Chat.Commands.StartConversation;
using Application.Chat.Queries.GetConversations;
using Application.Chat.Queries.GetMessages;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.KnowledgeBase.Models;
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
        result.Value.HasTicket.Should().BeFalse();
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
        var handler = new GetConversationsQueryHandler(new FakeCurrentUserService(currentUserId), conversations, new FakeTicketRepository());

        var result = await handler.Handle(new GetConversationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].Title.Should().Be("Mine");
    }


    [Fact]
    public async Task GetConversations_ConversationWithTicket_ReturnsHasTicket()
    {
        var currentUserId = Guid.NewGuid();
        var conversation = Conversation.Start(currentUserId, "Printer", DateTimeOffset.UtcNow);
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var tickets = new FakeTicketRepository();
        tickets.Items.Add(Ticket.Create(conversation.Id, Guid.NewGuid(), currentUserId, "Printer", "Printer issue", DateTimeOffset.UtcNow));
        var handler = new GetConversationsQueryHandler(new FakeCurrentUserService(currentUserId), conversations, tickets);

        var result = await handler.Handle(new GetConversationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].HasTicket.Should().BeTrue();
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
        var handler = new SendMessageCommandHandler(new FakeCurrentUserService(currentUserId), conversations, messages, new FakeKnowledgeBaseSearchService(), new FakeChatAiService(), new FakeMessageSourceRepository(), unitOfWork);

        var result = await handler.Handle(new SendMessageCommand(conversation.Id, " My keyboard is broken. "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        messages.Items.Should().HaveCount(2);
        messages.Items[0].Sender.Should().Be(MessageSender.User);
        messages.Items[0].Content.Should().Be("My keyboard is broken.");
        messages.Items[1].Sender.Should().Be(MessageSender.Assistant);
        result.Value!.AssistantMessage.Content.Should().Contain("could not find a matching knowledge-base article");
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task SendMessage_KnowledgeBaseMatch_PersistsRetrievalOnlyAssistantResponse()
    {
        var currentUserId = Guid.NewGuid();
        var conversation = Conversation.Start(currentUserId, "Wi-Fi", DateTimeOffset.UtcNow.AddMinutes(-1));
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var search = new FakeKnowledgeBaseSearchService(
            new KnowledgeBaseSearchResultItemDto(
                Guid.NewGuid(),
                "Office Wi-Fi Guide",
                "office-wifi.md",
                "text/markdown",
                Guid.NewGuid(),
                0,
                "Restart the access point and reconnect to Office-5G.",
                "Restart the access point and reconnect to Office-5G.",
                "fake-embedding",
                3,
                1));
        var messageSources = new FakeMessageSourceRepository();
        var handler = new SendMessageCommandHandler(new FakeCurrentUserService(currentUserId), conversations, messages, search, new FakeChatAiService("**1. Reconnect to Office-5G.**"), messageSources, unitOfWork);

        var result = await handler.Handle(new SendMessageCommand(conversation.Id, "wifi not working"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        search.Queries.Should().ContainSingle().Which.Should().Be("wifi not working");
        result.Value!.AssistantMessage.Content.Should().Contain("Office Wi-Fi Guide");
        result.Value.AssistantMessage.Content.Should().Contain("1. Reconnect to Office-5G.");
        result.Value.AssistantMessage.Content.Should().NotContain("**");
        result.Value.AssistantMessage.Content.Should().Contain("Sources:");
        messages.Items.Should().HaveCount(2);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task SendMessage_ChatAiFails_FallsBackToRetrievalOnlyResponse()
    {
        var currentUserId = Guid.NewGuid();
        var conversation = Conversation.Start(currentUserId, "Wi-Fi", DateTimeOffset.UtcNow.AddMinutes(-1));
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var search = new FakeKnowledgeBaseSearchService(
            new KnowledgeBaseSearchResultItemDto(
                Guid.NewGuid(),
                "Office Wi-Fi Guide",
                "office-wifi.md",
                "text/markdown",
                Guid.NewGuid(),
                0,
                "Restart the access point and reconnect to Office-5G.",
                "Restart the access point and reconnect to Office-5G.",
                "fake-embedding",
                3,
                1));
        var handler = new SendMessageCommandHandler(
            new FakeCurrentUserService(currentUserId),
            conversations,
            messages,
            search,
            new FakeChatAiService(shouldFail: true),
            new FakeMessageSourceRepository(),
            unitOfWork);

        var result = await handler.Handle(new SendMessageCommand(conversation.Id, "wifi not working"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AssistantMessage.Content.Should().Contain("AI answer service is unavailable");
        result.Value.AssistantMessage.Content.Should().Contain("Restart the access point");
        result.Value.AssistantMessage.Content.Should().Contain("Sources:");
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
            new FakeKnowledgeBaseSearchService(),
            new FakeChatAiService(),
            new FakeMessageSourceRepository(),
            new FakeUnitOfWork());

        var result = await handler.Handle(new SendMessageCommand(conversation.Id, "Help"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
    }

    [Fact]
    public async Task SendMessage_GreetingIntent_SkipsRetrievalAndReturnsCannedResponse()
    {
        var currentUserId = Guid.NewGuid();
        var conversation = Conversation.Start(currentUserId, "Help", DateTimeOffset.UtcNow.AddMinutes(-1));
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var search = new FakeKnowledgeBaseSearchService();
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SendMessageCommandHandler(
            new FakeCurrentUserService(currentUserId),
            conversations,
            messages,
            search,
            new FakeChatAiService(),
            new FakeMessageSourceRepository(),
            unitOfWork,
            new FakeIntentClassifierService(MessageIntent.Greeting));

        var result = await handler.Handle(new SendMessageCommand(conversation.Id, "Hello"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserMessage.Intent.Should().Be("Greeting");
        result.Value.AssistantMessage.Content.Should().Contain("Tell me what IT issue");
        search.Queries.Should().BeEmpty();
        unitOfWork.SaveCalls.Should().Be(1);
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
        var messageSources = new FakeMessageSourceRepository();
        var handler = new GetMessagesQueryHandler(new FakeCurrentUserService(currentUserId), conversations, messages, messageSources);

        var result = await handler.Handle(new GetMessagesQuery(conversation.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Content.Should().Be("First");
        result.Value[1].Content.Should().Be("Second");
    }



    [Fact]
    public async Task GetMessages_MessageWithSources_ReturnsSourceDocuments()
    {
        var currentUserId = Guid.NewGuid();
        var conversation = Conversation.Start(currentUserId, "Wi-Fi", DateTimeOffset.UtcNow.AddMinutes(-2));
        var assistant = conversation.AddMessage(MessageSender.Assistant, "Answer", null, DateTimeOffset.UtcNow);
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var messages = new FakeMessageRepository();
        messages.Items.Add(assistant);
        var messageSources = new FakeMessageSourceRepository(
            new MessageSourceReference(assistant.Id, Guid.NewGuid(), "Office Wi-Fi Guide", 4));
        var handler = new GetMessagesQueryHandler(new FakeCurrentUserService(currentUserId), conversations, messages, messageSources);

        var result = await handler.Handle(new GetMessagesQuery(conversation.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].SourceDocuments.Should().ContainSingle().Which.Should().Be("Office Wi-Fi Guide#chunk-4");
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
            new FakeTicketActivityRepository(),
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
            new FakeTicketActivityRepository(),
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
            new FakeTicketActivityRepository(),
            new FakeUnitOfWork());

        var result = await handler.Handle(new CreateTicketFromMessageCommand(conversation.Id, message.Id, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidMessage");
    }

    [Fact]
    public async Task CreateTicketFromMessage_ExistingTicketForConversation_ReturnsConflict()
    {
        var currentUserId = Guid.NewGuid();
        var conversation = Conversation.Start(currentUserId, "Help", DateTimeOffset.UtcNow.AddMinutes(-2));
        var message = conversation.AddMessage(MessageSender.User, "Need help", null, DateTimeOffset.UtcNow.AddMinutes(-1));
        var conversations = new FakeConversationRepository();
        conversations.Items.Add(conversation);
        var messages = new FakeMessageRepository();
        messages.Items.Add(message);
        var tickets = new FakeTicketRepository();
        tickets.Items.Add(Ticket.Create(conversation.Id, Guid.NewGuid(), currentUserId, "Existing ticket", "Existing ticket", DateTimeOffset.UtcNow));
        var handler = new CreateTicketFromMessageCommandHandler(
            new FakeCurrentUserService(currentUserId),
            conversations,
            messages,
            tickets,
            new FakeTicketActivityRepository(),
            new FakeUnitOfWork());

        var result = await handler.Handle(new CreateTicketFromMessageCommand(conversation.Id, message.Id, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TicketAlreadyExists");
    }
    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId, UserRole role = UserRole.Employee)
        {
            UserId = userId;
            Role = role;
        }

        public Guid UserId { get; }
        public UserRole Role { get; }
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



    private sealed class FakeKnowledgeBaseSearchService : IKnowledgeBaseSearchService
    {
        private readonly IReadOnlyList<KnowledgeBaseSearchResultItemDto> _items;

        public FakeKnowledgeBaseSearchService(params KnowledgeBaseSearchResultItemDto[] items)
        {
            _items = items;
        }

        public List<string> Queries { get; } = new();

        public Task<Result<KnowledgeBaseSearchResultDto>> SearchAsync(string query, int limit, CancellationToken cancellationToken)
        {
            Queries.Add(query.Trim());
            var result = new KnowledgeBaseSearchResultDto(query.Trim(), limit, _items.Take(limit).ToList());
            return Task.FromResult(Result<KnowledgeBaseSearchResultDto>.Success(result));
        }
    }

    private sealed class FakeIntentClassifierService : IIntentClassifierService
    {
        private readonly MessageIntent _intent;

        public FakeIntentClassifierService(MessageIntent intent)
        {
            _intent = intent;
        }

        public Task<MessageIntent> ClassifyAsync(string message, CancellationToken cancellationToken)
        {
            return Task.FromResult(_intent);
        }
    }
    private sealed class FakeChatAiService : IChatAiService
    {
        private readonly string _answer;
        private readonly bool _shouldFail;

        public FakeChatAiService(string answer = "AI answer", bool shouldFail = false)
        {
            _answer = answer;
            _shouldFail = shouldFail;
        }

        public Task<string> GenerateGroundedAnswerAsync(string question, IReadOnlyList<GroundingSource> sources, CancellationToken cancellationToken)
        {
            if (_shouldFail)
            {
                throw new InvalidOperationException("AI failed.");
            }

            return Task.FromResult(_answer);
        }
    }

    private sealed class FakeMessageSourceRepository : IMessageSourceRepository
    {
        private readonly IReadOnlyList<MessageSourceReference> _references;

        public FakeMessageSourceRepository(params MessageSourceReference[] references)
        {
            _references = references;
        }

        public List<MessageSource> Items { get; } = new();

        public Task<IReadOnlyList<MessageSourceReference>> ListByMessageIdsAsync(IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken)
        {
            IReadOnlyList<MessageSourceReference> result = _references
                .Where(source => messageIds.Contains(source.MessageId))
                .ToList();

            return Task.FromResult(result);
        }

        public Task AddRangeAsync(IReadOnlyCollection<MessageSource> sources, CancellationToken cancellationToken)
        {
            Items.AddRange(sources);
            return Task.CompletedTask;
        }
    }
    private sealed class FakeTicketRepository : ITicketRepository
    {
        public List<Ticket> Items { get; } = new();

        public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(ticket => ticket.Id == id));
        }

        public Task<Ticket?> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(ticket => ticket.ConversationId == conversationId));
        }

        public Task<IReadOnlyList<Ticket>> ListAsync(TicketListCriteria criteria, CancellationToken cancellationToken)
        {
            IReadOnlyList<Ticket> result = Items;
            return Task.FromResult(result);
        }

        public Task<int> CountAsync(TicketListCriteria criteria, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.Count);
        }

        public Task<IReadOnlySet<Guid>> ListConversationIdsWithTicketsAsync(IReadOnlyCollection<Guid> conversationIds, CancellationToken cancellationToken)
        {
            IReadOnlySet<Guid> result = Items
                .Where(ticket => conversationIds.Contains(ticket.ConversationId))
                .Select(ticket => ticket.ConversationId)
                .ToHashSet();

            return Task.FromResult(result);
        }

        public Task AddAsync(Ticket ticket, CancellationToken cancellationToken)
        {
            Items.Add(ticket);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTicketActivityRepository : ITicketActivityRepository
    {
        public List<TicketActivity> Items { get; } = new();

        public Task<IReadOnlyList<TicketActivity>> ListByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken)
        {
            IReadOnlyList<TicketActivity> result = Items.Where(activity => activity.TicketId == ticketId).ToList();
            return Task.FromResult(result);
        }

        public Task AddAsync(TicketActivity activity, CancellationToken cancellationToken)
        {
            Items.Add(activity);
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




