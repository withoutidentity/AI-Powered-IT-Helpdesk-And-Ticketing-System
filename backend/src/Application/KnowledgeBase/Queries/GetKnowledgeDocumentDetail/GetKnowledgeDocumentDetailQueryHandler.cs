using Application.Common.Interfaces;
using Application.Common.Models;
using Application.KnowledgeBase.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.KnowledgeBase.Queries.GetKnowledgeDocumentDetail;

public sealed class GetKnowledgeDocumentDetailQueryHandler : IRequestHandler<GetKnowledgeDocumentDetailQuery, Result<KnowledgeDocumentDetailDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IKnowledgeDocumentRepository _documents;
    private readonly IDocumentChunkRepository _chunks;

    public GetKnowledgeDocumentDetailQueryHandler(ICurrentUserService currentUser, IKnowledgeDocumentRepository documents, IDocumentChunkRepository chunks)
    {
        _currentUser = currentUser;
        _documents = documents;
        _chunks = chunks;
    }

    public async Task<Result<KnowledgeDocumentDetailDto>> Handle(GetKnowledgeDocumentDetailQuery request, CancellationToken cancellationToken)
    {
        if (!CanView())
        {
            return Result<KnowledgeDocumentDetailDto>.Failure("Forbidden", "Only IT agents and IT admins can view knowledge-base documents.");
        }

        var document = await _documents.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
        {
            return Result<KnowledgeDocumentDetailDto>.Failure("DocumentNotFound", "Knowledge-base document was not found.");
        }

        var chunks = await _chunks.ListByDocumentIdAsync(document.Id, cancellationToken);
        return Result<KnowledgeDocumentDetailDto>.Success(ToDetailDto(document, chunks));
    }

    private bool CanView()
    {
        return _currentUser.Role is UserRole.ITAgent or UserRole.ITAdmin;
    }

    private static KnowledgeDocumentDetailDto ToDetailDto(KnowledgeDocument document, IReadOnlyList<DocumentChunk> chunks)
    {
        return new KnowledgeDocumentDetailDto(
            document.Id,
            document.Title,
            document.SourceFile,
            document.SourceType,
            document.ContentHash,
            document.Status.ToString(),
            document.FailureReason,
            document.UploadedAt,
            document.UpdatedAt,
            chunks.OrderBy(chunk => chunk.ChunkIndex).Select(ToChunkDto).ToList());
    }

    private static DocumentChunkDto ToChunkDto(DocumentChunk chunk)
    {
        return new DocumentChunkDto(
            chunk.Id,
            chunk.DocumentId,
            chunk.ChunkIndex,
            chunk.Content,
            chunk.ContentHash,
            chunk.TokenCount,
            chunk.EmbeddingModel,
            chunk.EmbeddingDimensions,
            chunk.CreatedAt);
    }
}