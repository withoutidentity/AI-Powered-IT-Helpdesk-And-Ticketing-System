using FluentValidation;

namespace Application.KnowledgeBase.Queries.SearchKnowledgeBase;

public sealed class SearchKnowledgeBaseQueryValidator : AbstractValidator<SearchKnowledgeBaseQuery>
{
    public SearchKnowledgeBaseQueryValidator()
    {
        RuleFor(query => query.Query).NotEmpty().MaximumLength(1000);
        RuleFor(query => query.Limit).InclusiveBetween(1, 10);
    }
}