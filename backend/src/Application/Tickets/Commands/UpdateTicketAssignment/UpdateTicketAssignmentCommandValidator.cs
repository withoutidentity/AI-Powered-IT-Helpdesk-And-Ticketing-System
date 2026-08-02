using FluentValidation;

namespace Application.Tickets.Commands.UpdateTicketAssignment;

public sealed class UpdateTicketAssignmentCommandValidator : AbstractValidator<UpdateTicketAssignmentCommand>
{
    public UpdateTicketAssignmentCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.AssignedToUserId).NotEmpty();
    }
}
