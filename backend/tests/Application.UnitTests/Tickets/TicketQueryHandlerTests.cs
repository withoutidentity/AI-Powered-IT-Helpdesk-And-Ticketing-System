using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Tickets.Queries.GetTicketDetail;
using Application.Tickets.Queries.GetTickets;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Application.UnitTests.Tickets;

public sealed class TicketQueryHandlerTests
{
    [Fact]
    public async Task GetTickets_Employee_ReturnsOnlyOwnTickets()
    {
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var other = User.Create("other", "other@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var users = new FakeUserRepository(employee, other);
        var tickets = new FakeTicketRepository(
            CreateTicket(employee.Id, "Employee ticket"),
            CreateTicket(other.Id, "Other ticket"));
        var handler = new GetTicketsQueryHandler(new FakeCurrentUserService(employee.Id, UserRole.Employee), tickets, users);

        var result = await handler.Handle(new GetTicketsQuery(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items[0].Title.Should().Be("Employee ticket");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTickets_ITAgent_ReturnsAssignedAndUnassignedQueue()
    {
        var agent = User.Create("agent", "agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var otherAgent = User.Create("other.agent", "other.agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var assigned = CreateTicket(employee.Id, "Assigned");
        assigned.AssignTo(agent.Id, DateTimeOffset.UtcNow);
        var unassigned = CreateTicket(employee.Id, "Unassigned");
        var assignedToOther = CreateTicket(employee.Id, "Assigned to other");
        assignedToOther.AssignTo(otherAgent.Id, DateTimeOffset.UtcNow);
        var users = new FakeUserRepository(agent, otherAgent, employee);
        var tickets = new FakeTicketRepository(assigned, unassigned, assignedToOther);
        var handler = new GetTicketsQueryHandler(new FakeCurrentUserService(agent.Id, UserRole.ITAgent), tickets, users);

        var result = await handler.Handle(new GetTicketsQuery(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(ticket => ticket.Title).Should().BeEquivalentTo("Assigned", "Unassigned");
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTickets_ITAdmin_AppliesStatusAndPriorityFilters()
    {
        var admin = User.Create("admin", "admin@example.com", "hash", UserRole.ITAdmin, "IT", DateTimeOffset.UtcNow);
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var openHigh = CreateTicket(employee.Id, "Open high", TicketPriority.High);
        var openLow = CreateTicket(employee.Id, "Open low", TicketPriority.Low);
        var inProgressHigh = CreateTicket(employee.Id, "In progress high", TicketPriority.High);
        inProgressHigh.UpdateStatus(TicketStatus.InProgress, DateTimeOffset.UtcNow);
        var users = new FakeUserRepository(admin, employee);
        var tickets = new FakeTicketRepository(openHigh, openLow, inProgressHigh);
        var handler = new GetTicketsQueryHandler(new FakeCurrentUserService(admin.Id, UserRole.ITAdmin), tickets, users);

        var result = await handler.Handle(new GetTicketsQuery("Open", "High"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items[0].Title.Should().Be("Open high");
    }

    [Fact]
    public async Task GetTicketDetail_EmployeeOwnTicket_ReturnsTicket()
    {
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(employee.Id, "Own ticket");
        var handler = new GetTicketDetailQueryHandler(
            new FakeCurrentUserService(employee.Id, UserRole.Employee),
            new FakeTicketRepository(ticket),
            new FakeUserRepository(employee));

        var result = await handler.Handle(new GetTicketDetailQuery(ticket.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Own ticket");
        result.Value.CreatedBy.Username.Should().Be("employee");
    }

    [Fact]
    public async Task GetTicketDetail_EmployeeOtherTicket_ReturnsForbidden()
    {
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var other = User.Create("other", "other@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var ticket = CreateTicket(other.Id, "Other ticket");
        var handler = new GetTicketDetailQueryHandler(
            new FakeCurrentUserService(employee.Id, UserRole.Employee),
            new FakeTicketRepository(ticket),
            new FakeUserRepository(employee, other));

        var result = await handler.Handle(new GetTicketDetailQuery(ticket.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
    }

    [Fact]
    public async Task GetTicketDetail_MissingTicket_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var handler = new GetTicketDetailQueryHandler(
            new FakeCurrentUserService(userId, UserRole.ITAdmin),
            new FakeTicketRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(new GetTicketDetailQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TicketNotFound");
    }

    private static Ticket CreateTicket(Guid createdBy, string title, TicketPriority priority = TicketPriority.Medium)
    {
        return Ticket.Create(Guid.NewGuid(), Guid.NewGuid(), createdBy, title, title, DateTimeOffset.UtcNow, priority);
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
            IReadOnlyList<Ticket> result = ApplyCriteria(criteria)
                .OrderByDescending(ticket => ticket.CreatedAt)
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<int> CountAsync(TicketListCriteria criteria, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplyCriteria(criteria).Count());
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

        private IEnumerable<Ticket> ApplyCriteria(TicketListCriteria criteria)
        {
            IEnumerable<Ticket> query = Items;

            if (!criteria.IncludeAll)
            {
                if (criteria.CreatedBy is not null)
                {
                    query = query.Where(ticket => ticket.CreatedBy == criteria.CreatedBy);
                }
                else if (criteria.AssignedTo is not null)
                {
                    query = criteria.IncludeUnassigned
                        ? query.Where(ticket => ticket.AssignedTo == criteria.AssignedTo || ticket.AssignedTo == null)
                        : query.Where(ticket => ticket.AssignedTo == criteria.AssignedTo);
                }
                else if (criteria.IncludeUnassigned)
                {
                    query = query.Where(ticket => ticket.AssignedTo == null);
                }
            }

            if (criteria.Status is not null)
            {
                query = query.Where(ticket => ticket.Status == criteria.Status);
            }

            if (criteria.Priority is not null)
            {
                query = query.Where(ticket => ticket.Priority == criteria.Priority);
            }

            return query;
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
}
