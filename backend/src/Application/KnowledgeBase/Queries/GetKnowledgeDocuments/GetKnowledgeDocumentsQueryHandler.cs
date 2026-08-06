using Application.Common.Interfaces;
using Application.Common.Models;
using Application.KnowledgeBase.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.KnowledgeBase.Queries.GetKnowledgeDocuments;

public sealed class GetKnowledgeDocumentsQueryHandler : IRequestHandler<GetKnowledgeDocumentsQuery, Result<PaginatedList<KnowledgeDocumentSummaryDto>>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IKnowledgeDocumentRepository _documents;
    private readonly IDocumentChunkRepository _chunks;

    public GetKnowledgeDocumentsQueryHandler(ICurrentUserService currentUser, IKnowledgeDocumentRepository documents, IDocumentChunkRepository chunks)
    {
        _currentUser = currentUser;
        _documents = documents;
        _chunks = chunks;
    }

    public async Task<Result<PaginatedList<KnowledgeDocumentSummaryDto>>> Handle(GetKnowledgeDocumentsQuery request, CancellationToken cancellationToken)
    {
        if (!CanView())
        {
            return Result<PaginatedList<KnowledgeDocumentSummaryDto>>.Failure("Forbidden", "Only IT agents and IT admins can view knowledge-base documents.");
        }

        var documents = await _documents.ListAsync(request.Page, request.PageSize, cancellationToken);
        var totalCount = await _documents.CountAsync(cancellationToken);
        var items = new List<KnowledgeDocumentSummaryDto>();

        foreach (var document in documents)
        {
            var chunks = await _chunks.ListByDocumentIdAsync(document.Id, cancellationToken);
            items.Add(ToSummaryDto(document, chunks.Count));
        }

        return Result<PaginatedList<KnowledgeDocumentSummaryDto>>.Success(new PaginatedList<KnowledgeDocumentSummaryDto>(items, request.Page, request.PageSize, totalCount));
    }

    private bool CanView()
    {
        return _currentUser.Role is UserRole.ITAgent or UserRole.ITAdmin;
    }

    private static KnowledgeDocumentSummaryDto ToSummaryDto(KnowledgeDocument document, int chunkCount)
    {
        return new KnowledgeDocumentSummaryDto(
            document.Id,
            document.Title,
            document.SourceFile,
            document.SourceType,
            document.ContentHash,
            document.Status.ToString(),
            document.FailureReason,
            chunkCount,
            document.UploadedAt,
            document.UpdatedAt);
    }
}