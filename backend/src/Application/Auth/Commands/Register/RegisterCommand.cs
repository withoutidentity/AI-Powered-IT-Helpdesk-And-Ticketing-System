using Application.Auth.Models;
using Application.Common.Models;
using MediatR;

namespace Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string Role,
    string Department) : IRequest<Result<RegisterResponse>>;