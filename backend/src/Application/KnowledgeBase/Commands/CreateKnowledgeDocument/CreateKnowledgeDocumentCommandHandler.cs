using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.KnowledgeBase.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.KnowledgeBase.Commands.CreateKnowledgeDocument;

public sealed class CreateKnowledgeDocumentCommandHandler : IRequestHandler<CreateKnowledgeDocumentCommand, Result<KnowledgeDocumentDetailDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IKnowledgeDocumentRepository _documents;
    private readonly IDocumentChunkRepository _chunks;
    private readonly IKnowledgeDocumentChunker _chunker;
    private readonly IUnitOfWork _unitOfWork;

    public CreateKnowledgeDocumentCommandHandler(
        ICurrentUserService currentUser,
        IKnowledgeDocumentRepository documents,
        IDocumentChunkRepository chunks,
        IKnowledgeDocumentChunker chunker,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _documents = documents;
        _chunks = chunks;
        _chunker = chunker;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<KnowledgeDocumentDetailDto>> Handle(CreateKnowledgeDocumentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.ITAdmin)
        {
            return Result<KnowledgeDocumentDetailDto>.Failure("Forbidden", "Only IT admins can create knowledge-base documents.");
        }

        var now = DateTimeOffset.UtcNow;
        var normalizedContent = request.Content.Trim();
        var contentHash = ComputeSha256(normalizedContent);
        var document = KnowledgeDocument.Create(request.Title, request.SourceFile, request.SourceType, contentHash, now);
        var chunks = _chunker.Split(normalizedContent)
            .Select(chunk => DocumentChunk.Create(
                document.Id,
                chunk.ChunkIndex,
                chunk.Content,
                ComputeSha256(chunk.Content),
                now,
                chunk.TokenCount))
            .ToList();

        if (chunks.Count == 0)
        {
            return Result<KnowledgeDocumentDetailDto>.Failure("ValidationFailed", "Document content could not be split into chunks.");
        }

        document.MarkReady(now);
        await _documents.AddAsync(document, cancellationToken);
        foreach (var chunk in chunks)
        {
            await _chunks.AddAsync(chunk, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<KnowledgeDocumentDetailDto>.Success(ToDetailDto(document, chunks));
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
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
            chunks
                .OrderBy(chunk => chunk.ChunkIndex)
                .Select(chunk => new DocumentChunkDto(
                    chunk.Id,
                    chunk.DocumentId,
                    chunk.ChunkIndex,
                    chunk.Content,
                    chunk.ContentHash,
                    chunk.TokenCount,
                    chunk.EmbeddingModel,
                    chunk.EmbeddingDimensions,
                    chunk.CreatedAt))
                .ToList());
    }
}