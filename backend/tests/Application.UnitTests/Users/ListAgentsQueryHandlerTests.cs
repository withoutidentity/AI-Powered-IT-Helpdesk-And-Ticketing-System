using Application.Common.Interfaces;
using Application.Users.Queries.ListAgents;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Application.UnitTests.Users;

public sealed class ListAgentsQueryHandlerTests
{
    [Fact]
    public async Task ListAgents_ITAdmin_ReturnsOnlyITAgentsOrderedByUsername()
    {
        var admin = User.Create("admin", "admin@example.com", "hash", UserRole.ITAdmin, "IT", DateTimeOffset.UtcNow);
        var secondAgent = User.Create("z.agent", "z.agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var firstAgent = User.Create("a.agent", "a.agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var employee = User.Create("employee", "employee@example.com", "hash", UserRole.Employee, "IT", DateTimeOffset.UtcNow);
        var handler = new ListAgentsQueryHandler(
            new FakeCurrentUserService(admin.Id, UserRole.ITAdmin),
            new FakeUserRepository(admin, secondAgent, firstAgent, employee));

        var result = await handler.Handle(new ListAgentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(agent => agent.Username).Should().Equal("a.agent", "z.agent");
    }

    [Fact]
    public async Task ListAgents_ITAgent_ReturnsForbidden()
    {
        var agent = User.Create("agent", "agent@example.com", "hash", UserRole.ITAgent, "IT", DateTimeOffset.UtcNow);
        var handler = new ListAgentsQueryHandler(
            new FakeCurrentUserService(agent.Id, UserRole.ITAgent),
            new FakeUserRepository(agent));

        var result = await handler.Handle(new ListAgentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
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
