namespace Application.Auth.Models;

public sealed record RegisterResponse(Guid Id, string Username, string Email, string Role, string Department, DateTimeOffset CreatedAt);