using Application.Common.Models;
using Application.KnowledgeBase.Models;
using MediatR;

namespace Application.KnowledgeBase.Queries.SearchKnowledgeBase;

public sealed record SearchKnowledgeBaseQuery(string Query, int Limit = 5) : IRequest<Result<KnowledgeBaseSearchResultDto>>;