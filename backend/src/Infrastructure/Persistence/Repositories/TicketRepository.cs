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

    public Task<Ticket?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken)
    {
        return _dbContext.Tickets.FirstOrDefaultAsync(ticket => ticket.MessageId == messageId, cancellationToken);
    }

    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        await _dbContext.Tickets.AddAsync(ticket, cancellationToken);
    }
}