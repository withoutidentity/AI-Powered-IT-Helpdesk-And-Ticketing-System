using FluentValidation;

namespace Application.KnowledgeBase.Queries.GetKnowledgeDocumentDetail;

public sealed class GetKnowledgeDocumentDetailQueryValidator : AbstractValidator<GetKnowledgeDocumentDetailQuery>
{
    public GetKnowledgeDocumentDetailQueryValidator()
    {
        RuleFor(query => query.DocumentId).NotEmpty();
    }
}