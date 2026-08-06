using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IDocumentChunkRepository
{
    Task<IReadOnlyList<DocumentChunk>> ListByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken);
    Task AddAsync(DocumentChunk chunk, CancellationToken cancellationToken);
}