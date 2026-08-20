using Application.Common.Interfaces;
using Application.Common.Models;
using Application.KnowledgeBase.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.KnowledgeBase.Commands.ReindexKnowledgeDocument;

public sealed class ReindexKnowledgeDocumentCommandHandler : IRequestHandler<ReindexKnowledgeDocumentCommand, Result<ReindexKnowledgeDocumentResultDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IKnowledgeDocumentRepository _documents;
    private readonly IDocumentChunkRepository _chunks;
    private readonly IEmbeddingService _embeddingService;
    private readonly IUnitOfWork _unitOfWork;

    public ReindexKnowledgeDocumentCommandHandler(
        ICurrentUserService currentUser,
        IKnowledgeDocumentRepository documents,
        IDocumentChunkRepository chunks,
        IEmbeddingService embeddingService,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _documents = documents;
        _chunks = chunks;
        _embeddingService = embeddingService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReindexKnowledgeDocumentResultDto>> Handle(ReindexKnowledgeDocumentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.ITAdmin)
        {
            return Result<ReindexKnowledgeDocumentResultDto>.Failure("Forbidden", "Only IT admins can reindex knowledge-base documents.");
        }

        var document = await _documents.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
        {
            return Result<ReindexKnowledgeDocumentResultDto>.Failure("DocumentNotFound", "Knowledge-base document was not found.");
        }

        var chunks = await _chunks.ListByDocumentIdAsync(document.Id, cancellationToken);
        if (chunks.Count == 0)
        {
            return Result<ReindexKnowledgeDocumentResultDto>.Failure("NoChunks", "Knowledge-base document has no chunks to embed.");
        }

        var now = DateTimeOffset.UtcNow;
        document.MarkProcessing(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var embeddingModel = _embeddingService.Model;
            var embeddingDimensions = _embeddingService.Dimensions;
            var chunksToEmbed = chunks
                .OrderBy(chunk => chunk.ChunkIndex)
                .Where(chunk => !IsAlreadyEmbedded(chunk, embeddingModel, embeddingDimensions))
                .ToList();
            var embeddedCount = 0;

            foreach (var chunk in chunksToEmbed)
            {
                var embedding = await _embeddingService.EmbedDocumentAsync(chunk.Content, cancellationToken);
                if (embedding.Values.Count == 0)
                {
                    throw new InvalidOperationException("Embedding provider returned an empty vector.");
                }

                if (embedding.Values.Count != embedding.Dimensions)
                {
                    throw new InvalidOperationException("Embedding vector length does not match reported dimensions.");
                }

                if (embeddingDimensions is not null && embeddingDimensions.Value != embedding.Dimensions)
                {
                    throw new InvalidOperationException("Embedding provider returned inconsistent dimensions for chunks in the same document.");
                }

                embeddingModel = embedding.Model;
                embeddingDimensions = embedding.Dimensions;
                chunk.MarkEmbedded(embedding.Model, embedding.Dimensions);
                await _chunks.UpdateEmbeddingAsync(chunk.Id, embedding.Values, embedding.Model, embedding.Dimensions, cancellationToken);
                embeddedCount++;
            }

            now = DateTimeOffset.UtcNow;
            document.MarkReady(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ReindexKnowledgeDocumentResultDto>.Success(new ReindexKnowledgeDocumentResultDto(
                document.Id,
                document.Status.ToString(),
                embeddedCount,
                embeddingModel,
                embeddingDimensions ?? 0,
                document.UpdatedAt));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            now = DateTimeOffset.UtcNow;
            document.MarkFailed(ex.Message, now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<ReindexKnowledgeDocumentResultDto>.Failure("EmbeddingFailed", ex.Message);
        }
    }

    private static bool IsAlreadyEmbedded(DocumentChunk chunk, string embeddingModel, int? embeddingDimensions)
    {
        return !string.IsNullOrWhiteSpace(chunk.EmbeddingModel)
            && string.Equals(chunk.EmbeddingModel, embeddingModel, StringComparison.Ordinal)
            && embeddingDimensions is not null
            && chunk.EmbeddingDimensions == embeddingDimensions.Value;
    }
}



