using Application.Auth.Commands.Login;
using Application.Auth.Commands.Refresh;
using Application.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RegisterCommand(
            request.Username,
            request.Email,
            request.Password,
            request.Role,
            request.Department), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "UsernameAlreadyExists" or "EmailAlreadyExists" => ConflictProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Created($"/api/v1/users/{result.Value!.Id}", result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new LoginCommand(request.Username, request.Password), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "InvalidCredentials" => UnauthorizedProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RefreshCommand(request.RefreshToken), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "InvalidRefreshToken" => UnauthorizedProblem(result.ErrorMessage!),
                "ValidationFailed" => ValidationProblem(result.ErrorMessage!),
                _ => BadRequestProblem(result.ErrorMessage!)
            };
        }

        return Ok(result.Value);
    }

    private IActionResult BadRequestProblem(string detail)
    {
        return Problem(title: "Bad request", detail: detail, statusCode: StatusCodes.Status400BadRequest);
    }

    private IActionResult ValidationProblem(string detail)
    {
        return Problem(title: "Validation failed", detail: detail, statusCode: StatusCodes.Status400BadRequest);
    }

    private IActionResult ConflictProblem(string detail)
    {
        return Problem(title: "Conflict", detail: detail, statusCode: StatusCodes.Status409Conflict);
    }

    private IActionResult UnauthorizedProblem(string detail)
    {
        return Problem(title: "Unauthorized", detail: detail, statusCode: StatusCodes.Status401Unauthorized);
    }
}

public sealed record RegisterRequest(string Username, string Email, string Password, string Role, string Department);

public sealed record LoginRequest(string Username, string Password);

public sealed record RefreshRequest(string RefreshToken);