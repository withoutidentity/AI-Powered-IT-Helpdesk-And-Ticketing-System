using Application.Common.Models;
using Application.KnowledgeBase.Models;

namespace Application.Common.Interfaces;

public interface IKnowledgeBaseSearchService
{
    Task<Result<KnowledgeBaseSearchResultDto>> SearchAsync(string query, int limit, CancellationToken cancellationToken);
}