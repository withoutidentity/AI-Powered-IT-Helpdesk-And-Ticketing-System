using Application.Common.Interfaces;
using Application.Common.Models;
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

    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Tickets.FirstOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);
    }

    public Task<Ticket?> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return _dbContext.Tickets.FirstOrDefaultAsync(ticket => ticket.ConversationId == conversationId, cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> ListAsync(TicketListCriteria criteria, CancellationToken cancellationToken)
    {
        return await ApplyCriteria(_dbContext.Tickets.AsNoTracking(), criteria)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(TicketListCriteria criteria, CancellationToken cancellationToken)
    {
        return ApplyCriteria(_dbContext.Tickets.AsNoTracking(), criteria).CountAsync(cancellationToken);
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

    private static IQueryable<Ticket> ApplyCriteria(IQueryable<Ticket> query, TicketListCriteria criteria)
    {
        if (!criteria.IncludeAll)
        {
            if (criteria.CreatedBy is not null)
            {
                query = query.Where(ticket => ticket.CreatedBy == criteria.CreatedBy);
            }
            else if (criteria.AssignedTo is not null)
            {
                query = criteria.IncludeUnassigned
                    ? query.Where(ticket => ticket.AssignedTo == criteria.AssignedTo || ticket.AssignedTo == null)
                    : query.Where(ticket => ticket.AssignedTo == criteria.AssignedTo);
            }
            else if (criteria.IncludeUnassigned)
            {
                query = query.Where(ticket => ticket.AssignedTo == null);
            }
        }

        if (criteria.Status is not null)
        {
            query = query.Where(ticket => ticket.Status == criteria.Status);
        }

        if (criteria.Priority is not null)
        {
            query = query.Where(ticket => ticket.Priority == criteria.Priority);
        }

        return query;
    }
}
