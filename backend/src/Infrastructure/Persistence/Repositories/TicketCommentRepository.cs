using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class TicketCommentRepository : ITicketCommentRepository
{
    private readonly AppDbContext _dbContext;

    public TicketCommentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TicketComment>> ListByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        return await _dbContext.TicketComments
            .AsNoTracking()
            .Where(comment => comment.TicketId == ticketId)
            .OrderBy(comment => comment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TicketComment comment, CancellationToken cancellationToken)
    {
        await _dbContext.TicketComments.AddAsync(comment, cancellationToken);
    }
}
