using FluentValidation;

namespace Application.KnowledgeBase.Commands.CreateKnowledgeDocument;

public sealed class CreateKnowledgeDocumentCommandValidator : AbstractValidator<CreateKnowledgeDocumentCommand>
{
    public CreateKnowledgeDocumentCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.SourceFile).NotEmpty().MaximumLength(500);
        RuleFor(command => command.SourceType).NotEmpty().MaximumLength(80);
        RuleFor(command => command.Content).NotEmpty().MaximumLength(500_000);
    }
}