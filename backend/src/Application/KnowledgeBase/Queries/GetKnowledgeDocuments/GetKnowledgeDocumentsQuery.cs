using Application.Common.Models;
using Application.KnowledgeBase.Models;
using MediatR;

namespace Application.KnowledgeBase.Queries.GetKnowledgeDocuments;

public sealed record GetKnowledgeDocumentsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedList<KnowledgeDocumentSummaryDto>>>;