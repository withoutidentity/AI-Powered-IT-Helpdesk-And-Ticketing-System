namespace Application.Auth.Models;

public sealed record AuthUserDto(Guid Id, string Username, string Role);