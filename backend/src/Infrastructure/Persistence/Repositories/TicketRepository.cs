using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _dbContext;

    public TicketRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Ticket?> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return _dbContext.Tickets.FirstOrDefaultAsync(ticket => ticket.ConversationId == conversationId, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> ListConversationIdsWithTicketsAsync(IReadOnlyCollection<Guid> conversationIds, CancellationToken cancellationToken)
    {
        if (conversationIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var ids = await _dbContext.Tickets
            .Where(ticket => conversationIds.Contains(ticket.ConversationId))
            .Select(ticket => ticket.ConversationId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        await _dbContext.Tickets.AddAsync(ticket, cancellationToken);
    }
}