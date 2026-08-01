using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Tickets.Commands.UpdateTicketStatus;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Application.UnitTests.Tickets;

public sealed class TicketStatusCommandHandlerTests
{
    [Fact]
    public async Task UpdateTicketStatus_ITAgentUnassignedTicket_TransitionsOpenToInProgress()
    {
        var agent = User.Create("agent", "agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(employee.Id, "Printer");
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateTicketStatusCommandHandler(
            new FakeCurrentUserService(agent.Id, UserRole.ITAgent),
            new FakeTicketRepository(ticket),
            new FakeUserRepository(agent, employee),
            unitOfWork);

        var result = await handler.Handle(new UpdateTicketStatusCommand(ticket.Id, "InProgress"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("InProgress");
        ticket.Status.Should().Be(TicketStatus.InProgress);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task UpdateTicketStatus_ITAdminAssignedToOtherTicket_TransitionsInProgressToResolved()
    {
        var admin = User.Create("admin", "admin@example.com", "hash", UserRole.ITAdmin, "IT", DateTimeOffset.UtcNow);
        var agent = User.Create("agent", "agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(employee.Id, "Printer");
        ticket.AssignTo(agent.Id, DateTimeOffset.UtcNow);
        ticket.UpdateStatus(TicketStatus.InProgress, DateTimeOffset.UtcNow);
        var handler = new UpdateTicketStatusCommandHandler(
            new FakeCurrentUserService(admin.Id, UserRole.ITAdmin),
            new FakeTicketRepository(ticket),
            new FakeUserRepository(admin, agent, employee),
            new FakeUnitOfWork());

        var result = await handler.Handle(new UpdateTicketStatusCommand(ticket.Id, "Resolved"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Resolved");
    }

    [Fact]
    public async Task UpdateTicketStatus_EmployeeOwnTicket_ReturnsForbidden()
    {
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(employee.Id, "Printer");
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateTicketStatusCommandHandler(
            new FakeCurrentUserService(employee.Id, UserRole.Employee),
            new FakeTicketRepository(ticket),
            new FakeUserRepository(employee),
            unitOfWork);

        var result = await handler.Handle(new UpdateTicketStatusCommand(ticket.Id, "InProgress"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
        ticket.Status.Should().Be(TicketStatus.Open);
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpdateTicketStatus_ITAgentAssignedToOtherTicket_ReturnsForbidden()
    {
        var agent = User.Create("agent", "agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var otherAgent = User.Create("other.agent", "other.agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(employee.Id, "Printer");
        ticket.AssignTo(otherAgent.Id, DateTimeOffset.UtcNow);
        var handler = new UpdateTicketStatusCommandHandler(
            new FakeCurrentUserService(agent.Id, UserRole.ITAgent),
            new FakeTicketRepository(ticket),
            new FakeUserRepository(agent, otherAgent, employee),
            new FakeUnitOfWork());

        var result = await handler.Handle(new UpdateTicketStatusCommand(ticket.Id, "InProgress"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
    }

    [Fact]
    public async Task UpdateTicketStatus_OpenToResolved_ReturnsInvalidStatusTransition()
    {
        var admin = User.Create("admin", "admin@example.com", "hash", UserRole.ITAdmin, "IT", DateTimeOffset.UtcNow);
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(employee.Id, "Printer");
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateTicketStatusCommandHandler(
            new FakeCurrentUserService(admin.Id, UserRole.ITAdmin),
            new FakeTicketRepository(ticket),
            new FakeUserRepository(admin, employee),
            unitOfWork);

        var result = await handler.Handle(new UpdateTicketStatusCommand(ticket.Id, "Resolved"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidStatusTransition");
        ticket.Status.Should().Be(TicketStatus.Open);
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpdateTicketStatus_MissingTicket_ReturnsNotFound()
    {
        var adminId = Guid.NewGuid();
        var handler = new UpdateTicketStatusCommandHandler(
            new FakeCurrentUserService(adminId, UserRole.ITAdmin),
            new FakeTicketRepository(),
            new FakeUserRepository(),
            new FakeUnitOfWork());

        var result = await handler.Handle(new UpdateTicketStatusCommand(Guid.NewGuid(), "InProgress"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TicketNotFound");
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
