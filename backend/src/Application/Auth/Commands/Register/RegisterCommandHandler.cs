using Application.Auth.Models;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(IUserRepository users, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.GetByUsernameAsync(username, cancellationToken) is not null)
        {
            return Result<RegisterResponse>.Failure("UsernameAlreadyExists", "Username already exists.");
        }

        if (await _users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return Result<RegisterResponse>.Failure("EmailAlreadyExists", "Email already exists.");
        }

        var role = Enum.Parse<UserRole>(request.Role, true);
        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(username, email, passwordHash, role, request.Department, DateTimeOffset.UtcNow);

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RegisterResponse>.Success(new RegisterResponse(
            user.Id,
            user.Username,
            user.Email,
            user.Role.ToString(),
            user.Department,
            user.CreatedAt));
    }
}