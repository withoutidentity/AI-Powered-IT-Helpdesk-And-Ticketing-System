using Application.Auth.Models;
using Application.Common.Models;
using MediatR;

namespace Application.Auth.Commands.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<Result<LoginResponse>>;