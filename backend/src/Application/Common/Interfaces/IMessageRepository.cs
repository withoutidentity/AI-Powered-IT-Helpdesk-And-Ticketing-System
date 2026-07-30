using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IMessageRepository
{
    Task<IReadOnlyList<Message>> ListByConversationAsync(Guid conversationId, CancellationToken cancellationToken);
    Task AddAsync(Message message, CancellationToken cancellationToken);
}