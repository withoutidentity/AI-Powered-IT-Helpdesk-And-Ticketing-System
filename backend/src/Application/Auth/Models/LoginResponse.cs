namespace Application.Auth.Models;

public sealed record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, AuthUserDto User);