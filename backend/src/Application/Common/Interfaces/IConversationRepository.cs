using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Conversation>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken);
}