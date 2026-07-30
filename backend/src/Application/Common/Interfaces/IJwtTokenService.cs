using Application.Auth.Models;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IJwtTokenService
{
    TokenResult CreateToken(User user);
    string HashRefreshToken(string refreshToken);
}