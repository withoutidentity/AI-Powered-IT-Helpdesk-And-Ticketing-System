using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Message>> ListByConversationAsync(Guid conversationId, CancellationToken cancellationToken);
    Task AddAsync(Message message, CancellationToken cancellationToken);
}