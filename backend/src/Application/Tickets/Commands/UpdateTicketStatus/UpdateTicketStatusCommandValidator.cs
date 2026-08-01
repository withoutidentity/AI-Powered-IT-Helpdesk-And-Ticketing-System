using Domain.Enums;
using FluentValidation;

namespace Application.Tickets.Commands.UpdateTicketStatus;

public sealed class UpdateTicketStatusCommandValidator : AbstractValidator<UpdateTicketStatusCommand>
{
    public UpdateTicketStatusCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();

        RuleFor(command => command.Status)
            .NotEmpty()
            .Must(status => Enum.TryParse<TicketStatus>(status, ignoreCase: true, out _))
            .WithMessage("Status must be one of: Open, InProgress, Resolved, Closed.");
    }
}
