using Application.Auth.Commands.Login;
using Application.Auth.Commands.Refresh;
using Application.Auth.Commands.Register;
using Application.Auth.Models;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Application.UnitTests.Auth;

public sealed class AuthCommandHandlerTests
{
    [Fact]
    public async Task Register_NewUser_CreatesUser()
    {
        var users = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegisterCommandHandler(users, new FakePasswordHasher(), unitOfWork);

        var result = await handler.Handle(new RegisterCommand(
            "jane.doe",
            "Jane.Doe@Company.com",
            "P@ssw0rd123!",
            "Employee",
            "Finance"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Username.Should().Be("jane.doe");
        result.Value.Email.Should().Be("jane.doe@company.com");
        result.Value.Role.Should().Be("Employee");
        users.Items.Should().ContainSingle();
        users.Items[0].PasswordHash.Should().Be("hashed:P@ssw0rd123!");
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsFailure()
    {
        var users = new FakeUserRepository();
        users.Items.Add(User.Create("jane.doe", "jane@company.com", "hash", UserRole.Employee, "Finance", DateTimeOffset.UtcNow));
        var handler = new RegisterCommandHandler(users, new FakePasswordHasher(), new FakeUnitOfWork());

        var result = await handler.Handle(new RegisterCommand(
            "jane.doe",
            "other@company.com",
            "P@ssw0rd123!",
            "Employee",
            "Finance"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("UsernameAlreadyExists");
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenResponseAndPersistsRefreshToken()
    {
        var users = new FakeUserRepository();
        users.Items.Add(User.Create("jane.doe", "jane@company.com", "hashed:P@ssw0rd123!", UserRole.Employee, "Finance", DateTimeOffset.UtcNow));
        var refreshTokens = new FakeRefreshTokenRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new LoginCommandHandler(users, refreshTokens, new FakePasswordHasher(), new FakeJwtTokenService(), unitOfWork);

        var result = await handler.Handle(new LoginCommand("jane.doe", "P@ssw0rd123!"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresIn.Should().Be(900);
        result.Value.User.Username.Should().Be("jane.doe");
        refreshTokens.Items.Should().ContainSingle();
        refreshTokens.Items[0].TokenHash.Should().Be("hash:refresh-token");
        refreshTokens.Items[0].IsActive(DateTimeOffset.UtcNow).Should().BeTrue();
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsInvalidCredentials()
    {
        var users = new FakeUserRepository();
        users.Items.Add(User.Create("jane.doe", "jane@company.com", "hashed:P@ssw0rd123!", UserRole.Employee, "Finance", DateTimeOffset.UtcNow));
        var handler = new LoginCommandHandler(users, new FakeRefreshTokenRepository(), new FakePasswordHasher(), new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(new LoginCommand("jane.doe", "wrong-password"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidCredentials");
    }

    [Fact]
    public async Task Refresh_ActiveRefreshToken_RotatesRefreshToken()
    {
        var user = User.Create("jane.doe", "jane@company.com", "hash", UserRole.Employee, "Finance", DateTimeOffset.UtcNow);
        var users = new FakeUserRepository();
        users.Items.Add(user);
        var refreshTokens = new FakeRefreshTokenRepository();
        var existingRefreshToken = RefreshToken.Create(user.Id, "hash:old-refresh-token", DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddMinutes(-5));
        refreshTokens.Items.Add(existingRefreshToken);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RefreshCommandHandler(refreshTokens, users, new FakeJwtTokenService(), unitOfWork);

        var result = await handler.Handle(new RefreshCommand("old-refresh-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        existingRefreshToken.RevokedAt.Should().NotBeNull();
        existingRefreshToken.ReplacedByTokenHash.Should().Be("hash:refresh-token");
        refreshTokens.Items.Should().HaveCount(2);
        refreshTokens.Items.Should().Contain(token => token.TokenHash == "hash:refresh-token");
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Refresh_UnknownRefreshToken_ReturnsInvalidRefreshToken()
    {
        var handler = new RefreshCommandHandler(
            new FakeRefreshTokenRepository(),
            new FakeUserRepository(),
            new FakeJwtTokenService(),
            new FakeUnitOfWork());

        var result = await handler.Handle(new RefreshCommand("missing-refresh-token"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidRefreshToken");
    }

    private sealed class FakeUserRepository : IUserRepository
    {
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

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> Items { get; } = new();

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(refreshToken => refreshToken.TokenHash == tokenHash));
        }

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            Items.Add(refreshToken);
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

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return $"hashed:{password}";
        }

        public bool Verify(string password, string passwordHash)
        {
            return passwordHash == Hash(password);
        }
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public TokenResult CreateToken(User user)
        {
            return new TokenResult("access-token", "refresh-token", 900, DateTimeOffset.UtcNow.AddDays(7));
        }

        public string HashRefreshToken(string refreshToken)
        {
            return $"hash:{refreshToken}";
        }
    }
}
