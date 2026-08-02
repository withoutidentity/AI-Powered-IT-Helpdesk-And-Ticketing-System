using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ITicketActivityRepository
{
    Task<IReadOnlyList<TicketActivity>> ListByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken);
    Task AddAsync(TicketActivity activity, CancellationToken cancellationToken);
}
