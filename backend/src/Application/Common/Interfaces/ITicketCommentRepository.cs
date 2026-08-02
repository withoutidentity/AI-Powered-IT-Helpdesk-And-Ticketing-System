using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ITicketCommentRepository
{
    Task<IReadOnlyList<TicketComment>> ListByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken);
    Task AddAsync(TicketComment comment, CancellationToken cancellationToken);
}
