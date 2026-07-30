using FluentValidation;

namespace Application.Chat.Commands.StartConversation;

public sealed class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationCommandValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200);
    }
}