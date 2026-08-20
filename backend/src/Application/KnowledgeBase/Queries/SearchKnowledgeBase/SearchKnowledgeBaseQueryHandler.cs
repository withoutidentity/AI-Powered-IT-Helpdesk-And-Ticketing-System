using Application.Common.Interfaces;
using Application.Common.Models;
using Application.KnowledgeBase.Models;
using Domain.Enums;
using MediatR;

namespace Application.KnowledgeBase.Queries.SearchKnowledgeBase;

public sealed class SearchKnowledgeBaseQueryHandler : IRequestHandler<SearchKnowledgeBaseQuery, Result<KnowledgeBaseSearchResultDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IKnowledgeBaseSearchService _search;

    public SearchKnowledgeBaseQueryHandler(ICurrentUserService currentUser, IKnowledgeBaseSearchService search)
    {
        _currentUser = currentUser;
        _search = search;
    }

    public Task<Result<KnowledgeBaseSearchResultDto>> Handle(SearchKnowledgeBaseQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role is not (UserRole.ITAgent or UserRole.ITAdmin))
        {
            return Task.FromResult(Result<KnowledgeBaseSearchResultDto>.Failure("Forbidden", "Only IT agents and IT admins can search the knowledge base."));
        }

        return _search.SearchAsync(request.Query, request.Limit, cancellationToken);
    }
}