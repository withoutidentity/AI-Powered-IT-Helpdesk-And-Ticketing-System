using Application.Common.Models;
using Application.KnowledgeBase.Models;
using MediatR;

namespace Application.KnowledgeBase.Queries.GetKnowledgeDocumentDetail;

public sealed record GetKnowledgeDocumentDetailQuery(Guid DocumentId) : IRequest<Result<KnowledgeDocumentDetailDto>>;