using FluentValidation;

namespace Application.KnowledgeBase.Queries.GetKnowledgeDocuments;

public sealed class GetKnowledgeDocumentsQueryValidator : AbstractValidator<GetKnowledgeDocumentsQuery>
{
    public GetKnowledgeDocumentsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}