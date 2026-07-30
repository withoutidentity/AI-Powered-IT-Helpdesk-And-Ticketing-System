using Application.Auth.Models;
using Application.Common.Models;
using MediatR;

namespace Application.Auth.Commands.Login;

public sealed record LoginCommand(string Username, string Password) : IRequest<Result<LoginResponse>>;