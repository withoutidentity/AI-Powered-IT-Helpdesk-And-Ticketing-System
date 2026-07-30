namespace Application.Auth.Models;

public sealed record TokenResult(string AccessToken, string RefreshToken, int ExpiresIn, DateTimeOffset RefreshTokenExpiresAt);