using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class TicketActivityRepository : ITicketActivityRepository
{
    private readonly AppDbContext _dbContext;

    public TicketActivityRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TicketActivity>> ListByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        return await _dbContext.TicketActivities
            .AsNoTracking()
            .Where(activity => activity.TicketId == ticketId)
            .OrderBy(activity => activity.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TicketActivity activity, CancellationToken cancellationToken)
    {
        await _dbContext.TicketActivities.AddAsync(activity, cancellationToken);
    }
}
