using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class MessageSourceRepository : IMessageSourceRepository
{
    private readonly AppDbContext _dbContext;

    public MessageSourceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MessageSourceReference>> ListByMessageIdsAsync(IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return [];
        }

        return await (
            from source in _dbContext.MessageSources.AsNoTracking()
            join chunk in _dbContext.DocumentChunks.AsNoTracking() on source.DocumentChunkId equals chunk.Id
            join document in _dbContext.KnowledgeDocuments.AsNoTracking() on chunk.DocumentId equals document.Id
            where messageIds.Contains(source.MessageId)
            orderby source.CreatedAt, chunk.ChunkIndex
            select new MessageSourceReference(
                source.MessageId,
                source.DocumentChunkId,
                document.Title,
                chunk.ChunkIndex))
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyCollection<MessageSource> sources, CancellationToken cancellationToken)
    {
        if (sources.Count == 0)
        {
            return;
        }

        await _dbContext.MessageSources.AddRangeAsync(sources, cancellationToken);
    }
}