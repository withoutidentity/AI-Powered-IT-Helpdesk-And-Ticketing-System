using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class KnowledgeDocumentRepository : IKnowledgeDocumentRepository
{
    private readonly AppDbContext _dbContext;

    public KnowledgeDocumentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<KnowledgeDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeDocuments.AsNoTracking().FirstOrDefaultAsync(document => document.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return await _dbContext.KnowledgeDocuments
            .AsNoTracking()
            .OrderByDescending(document => document.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        return await _dbContext.KnowledgeDocuments
            .AsNoTracking()
            .Where(document => ids.Contains(document.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeDocuments.AsNoTracking().CountAsync(cancellationToken);
    }

    public async Task AddAsync(KnowledgeDocument document, CancellationToken cancellationToken)
    {
        await _dbContext.KnowledgeDocuments.AddAsync(document, cancellationToken);
    }
}