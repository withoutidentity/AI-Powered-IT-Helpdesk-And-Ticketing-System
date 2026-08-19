using FluentValidation;

namespace Application.KnowledgeBase.Commands.ReindexKnowledgeDocument;

public sealed class ReindexKnowledgeDocumentCommandValidator : AbstractValidator<ReindexKnowledgeDocumentCommand>
{
    public ReindexKnowledgeDocumentCommandValidator()
    {
        RuleFor(command => command.DocumentId).NotEmpty();
    }
}
