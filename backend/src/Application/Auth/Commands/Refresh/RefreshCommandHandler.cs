using Application.Auth.Models;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;

namespace Application.Auth.Commands.Refresh;

public sealed class RefreshCommandHandler : IRequestHandler<RefreshCommand, Result<LoginResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUserRepository users,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponse>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var existingRefreshToken = await _refreshTokens.GetByTokenHashAsync(refreshTokenHash, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (existingRefreshToken is null || !existingRefreshToken.IsActive(now))
        {
            return Result<LoginResponse>.Failure("InvalidRefreshToken", "Invalid or expired refresh token.");
        }

        var user = await _users.GetByIdAsync(existingRefreshToken.UserId, cancellationToken);
        if (user is null)
        {
            return Result<LoginResponse>.Failure("InvalidRefreshToken", "Invalid or expired refresh token.");
        }

        var token = _jwtTokenService.CreateToken(user);
        var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(token.RefreshToken);
        var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenHash, token.RefreshTokenExpiresAt, now);

        existingRefreshToken.Revoke(now, newRefreshTokenHash);
        await _refreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            token.AccessToken,
            token.RefreshToken,
            token.ExpiresIn,
            new AuthUserDto(user.Id, user.Username, user.Role.ToString())));
    }
}