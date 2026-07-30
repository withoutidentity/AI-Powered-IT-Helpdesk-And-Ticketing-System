using Domain.Enums;

namespace Domain.Entities;

public sealed class User
{
    private User()
    {
        Username = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        Department = string.Empty;
    }

    private User(Guid id, string username, string email, string passwordHash, UserRole role, string department, DateTimeOffset createdAt)
    {
        Id = id;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        Department = department;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public string Department { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static User Create(string username, string email, string passwordHash, UserRole role, string department, DateTimeOffset createdAt)
    {
        return new User(
            Guid.NewGuid(),
            username.Trim(),
            email.Trim().ToLowerInvariant(),
            passwordHash,
            role,
            department.Trim(),
            createdAt);
    }
}