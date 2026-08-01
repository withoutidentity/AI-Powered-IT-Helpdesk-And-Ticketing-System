using Domain.Enums;
using FluentValidation;

namespace Application.Chat.Commands.CreateTicketFromMessage;

public sealed class CreateTicketFromMessageCommandValidator : AbstractValidator<CreateTicketFromMessageCommand>
{
    public CreateTicketFromMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title is not null);
        RuleFor(x => x.Description).MaximumLength(4000).When(x => x.Description is not null);
        RuleFor(x => x.Priority)
            .Must(priority => string.IsNullOrWhiteSpace(priority) || Enum.TryParse<TicketPriority>(priority, true, out _))
            .WithMessage("Priority must be one of Low, Medium, or High.");
    }
}