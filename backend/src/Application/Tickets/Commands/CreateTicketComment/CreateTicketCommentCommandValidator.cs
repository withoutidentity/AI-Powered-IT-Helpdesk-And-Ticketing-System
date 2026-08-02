using FluentValidation;

namespace Application.Tickets.Commands.CreateTicketComment;

public sealed class CreateTicketCommentCommandValidator : AbstractValidator<CreateTicketCommentCommand>
{
    public CreateTicketCommentCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Content)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
