using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IDocumentChunkRepository
{
    Task<IReadOnlyList<DocumentChunk>> ListByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentChunk>> SearchSimilarAsync(IReadOnlyList<float> embedding, int limit, CancellationToken cancellationToken);
    Task AddAsync(DocumentChunk chunk, CancellationToken cancellationToken);
    Task UpdateEmbeddingAsync(Guid chunkId, IReadOnlyList<float> embedding, string embeddingModel, int embeddingDimensions, CancellationToken cancellationToken);
}
