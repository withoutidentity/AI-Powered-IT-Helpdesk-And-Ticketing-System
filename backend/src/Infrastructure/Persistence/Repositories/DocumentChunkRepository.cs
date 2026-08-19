using System.Globalization;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly AppDbContext _dbContext;

    public DocumentChunkRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DocumentChunk>> ListByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return await _dbContext.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.DocumentId == documentId)
            .OrderBy(chunk => chunk.ChunkIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> SearchSimilarAsync(IReadOnlyList<float> embedding, int limit, CancellationToken cancellationToken)
    {
        if (embedding.Count == 0)
        {
            return [];
        }

        var vector = ToPgVectorLiteral(embedding);
        return await _dbContext.DocumentChunks
            .FromSqlInterpolated($@"
                SELECT id, document_id, chunk_index, content, content_hash, token_count, embedding_model, embedding_dimensions, created_at
                FROM document_chunks
                WHERE embedding IS NOT NULL
                ORDER BY embedding <=> {vector}::vector
                LIMIT {limit}")
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DocumentChunk chunk, CancellationToken cancellationToken)
    {
        await _dbContext.DocumentChunks.AddAsync(chunk, cancellationToken);
    }

    public async Task UpdateEmbeddingAsync(Guid chunkId, IReadOnlyList<float> embedding, string embeddingModel, int embeddingDimensions, CancellationToken cancellationToken)
    {
        if (embedding.Count != embeddingDimensions)
        {
            throw new ArgumentException("Embedding vector length must match embedding dimensions.", nameof(embedding));
        }

        var vector = ToPgVectorLiteral(embedding);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE document_chunks
            SET embedding = {vector}::vector,
                embedding_model = {embeddingModel},
                embedding_dimensions = {embeddingDimensions}
            WHERE id = {chunkId}", cancellationToken);
    }

    private static string ToPgVectorLiteral(IReadOnlyList<float> values)
    {
        return "[" + string.Join(',', values.Select(value => value.ToString("R", CultureInfo.InvariantCulture))) + "]";
    }
}
