using Domain.Enums;
using FluentValidation;

namespace Application.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => Enum.TryParse<UserRole>(role, true, out _))
            .WithMessage("Role must be one of Employee, ITAgent, or ITAdmin.");
        RuleFor(x => x.Department).NotEmpty().MaximumLength(120);
    }
}