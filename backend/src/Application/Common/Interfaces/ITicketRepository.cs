using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Ticket?> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Ticket>> ListAsync(TicketListCriteria criteria, CancellationToken cancellationToken);
    Task<int> CountAsync(TicketListCriteria criteria, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> ListConversationIdsWithTicketsAsync(IReadOnlyCollection<Guid> conversationIds, CancellationToken cancellationToken);
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken);
}
