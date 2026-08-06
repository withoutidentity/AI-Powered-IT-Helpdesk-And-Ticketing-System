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

    public async Task AddAsync(DocumentChunk chunk, CancellationToken cancellationToken)
    {
        await _dbContext.DocumentChunks.AddAsync(chunk, cancellationToken);
    }
}