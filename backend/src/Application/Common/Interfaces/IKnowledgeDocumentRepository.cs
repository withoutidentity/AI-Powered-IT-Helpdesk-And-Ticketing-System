using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IKnowledgeDocumentRepository
{
    Task<KnowledgeDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeDocument>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeDocument>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task AddAsync(KnowledgeDocument document, CancellationToken cancellationToken);
}