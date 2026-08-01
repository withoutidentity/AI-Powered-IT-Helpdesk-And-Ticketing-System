using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken);
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken);
}