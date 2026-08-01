using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> ListConversationIdsWithTicketsAsync(IReadOnlyCollection<Guid> conversationIds, CancellationToken cancellationToken);
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken);
}