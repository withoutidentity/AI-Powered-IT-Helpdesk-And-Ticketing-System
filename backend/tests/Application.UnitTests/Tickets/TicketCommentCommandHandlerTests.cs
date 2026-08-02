using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Tickets.Commands.CreateTicketComment;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Application.UnitTests.Tickets;

public sealed class TicketCommentCommandHandlerTests
{
    [Fact]
    public async Task CreateTicketComment_EmployeeOwnTicket_AddsComment()
    {
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(employee.Id, "Printer");
        var comments = new FakeTicketCommentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(employee, ticket, comments, unitOfWork, employee);

        var result = await handler.Handle(new CreateTicketCommentCommand(ticket.Id, " Please check this. "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Be("Please check this.");
        result.Value.Author.Id.Should().Be(employee.Id);
        comments.Items.Should().ContainSingle(comment => comment.TicketId == ticket.Id && comment.AuthorId == employee.Id);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task CreateTicketComment_EmployeeOtherTicket_ReturnsForbidden()
    {
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var otherEmployee = User.Create("other.employee", "other.employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(otherEmployee.Id, "Printer");
        var comments = new FakeTicketCommentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(employee, ticket, comments, unitOfWork, employee, otherEmployee);

        var result = await handler.Handle(new CreateTicketCommentCommand(ticket.Id, "Can I see this?"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
        comments.Items.Should().BeEmpty();
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task CreateTicketComment_ITAgentUnassignedTicket_AddsComment()
    {
        var agent = User.Create("agent", "agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(employee.Id, "Printer");
        var comments = new FakeTicketCommentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(agent, ticket, comments, unitOfWork, agent, employee);

        var result = await handler.Handle(new CreateTicketCommentCommand(ticket.Id, "Taking a look."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Author.Id.Should().Be(agent.Id);
        comments.Items.Should().ContainSingle(comment => comment.AuthorId == agent.Id);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task CreateTicketComment_ITAgentAssignedToSelf_AddsComment()
    {
        var agent = User.Create("agent", "agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(employee.Id, "Printer");
        ticket.AssignTo(agent.Id, DateTimeOffset.UtcNow);
        var comments = new FakeTicketCommentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(agent, ticket, comments, unitOfWork, agent, employee);

        var result = await handler.Handle(new CreateTicketCommentCommand(ticket.Id, "Work started."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        comments.Items.Should().ContainSingle(comment => comment.AuthorId == agent.Id);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task CreateTicketComment_ITAgentAssignedToOtherAgent_ReturnsForbidden()
    {
        var agent = User.Create("agent", "agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var otherAgent = User.Create("other.agent", "other.agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(employee.Id, "Printer");
        ticket.AssignTo(otherAgent.Id, DateTimeOffset.UtcNow);
        var comments = new FakeTicketCommentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(agent, ticket, comments, unitOfWork, agent, otherAgent, employee);

        var result = await handler.Handle(new CreateTicketCommentCommand(ticket.Id, "Can I comment?"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
        comments.Items.Should().BeEmpty();
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task CreateTicketComment_ITAdminAnyTicket_AddsComment()
    {
        var admin = User.Create("admin", "admin@example.com", "hash", UserRole.ITAdmin, "IT", DateTimeOffset.UtcNow);
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(employee.Id, "Printer");
        var comments = new FakeTicketCommentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(admin, ticket, comments, unitOfWork, admin, employee);

        var result = await handler.Handle(new CreateTicketCommentCommand(ticket.Id, "Admin note."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        comments.Items.Should().ContainSingle(comment => comment.AuthorId == admin.Id);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    private static CreateTicketCommentCommandHandler CreateHandler(
        User currentUser,
        Ticket ticket,
        FakeTicketCommentRepository comments,
        FakeUnitOfWork unitOfWork,
        params User[] users)
    {
        return new CreateTicketCommentCommandHandler(
            new FakeCurrentUserService(currentUser.Id, currentUser.Role),
            new FakeTicketRepository(ticket),
            comments,
            new FakeUserRepository(users),
            unitOfWork);
    }

    private static Ticket CreateTicket(Guid createdBy, string title)
    {
        return Ticket.Create(Guid.NewGuid(), Guid.NewGuid(), createdBy, title, title, DateTimeOffset.UtcNow);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId, UserRole role)
        {
            UserId = userId;
            Role = role;
        }

        public Guid UserId { get; }
        public UserRole Role { get; }
    }

    private sealed class FakeTicketRepository : ITicketRepository
    {
        public FakeTicketRepository(params Ticket[] tickets)
        {
            Items.AddRange(tickets);
        }

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

    private sealed class FakeTicketCommentRepository : ITicketCommentRepository
    {
        public List<TicketComment> Items { get; } = new();

        public Task<IReadOnlyList<TicketComment>> ListByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken)
        {
            IReadOnlyList<TicketComment> result = Items.Where(comment => comment.TicketId == ticketId).ToList();
            return Task.FromResult(result);
        }

        public Task AddAsync(TicketComment comment, CancellationToken cancellationToken)
        {
            Items.Add(comment);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public FakeUserRepository(params User[] users)
        {
            Items.AddRange(users);
        }

        public List<User> Items { get; } = new();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(user => user.Id == id));
        }

        public Task<IReadOnlyList<User>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
        {
            IReadOnlyList<User> result = Items.Where(user => ids.Contains(user.Id)).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<User>> ListByRoleAsync(UserRole role, CancellationToken cancellationToken)
        {
            IReadOnlyList<User> result = Items.Where(user => user.Role == role).ToList();
            return Task.FromResult(result);
        }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(user => user.Username == username.Trim()));
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return Task.FromResult(Items.FirstOrDefault(user => user.Email == normalizedEmail));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Items.Add(user);
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