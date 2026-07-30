using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _dbContext;

    public ConversationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Conversations.FirstOrDefaultAsync(conversation => conversation.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Conversations
            .Where(conversation => conversation.UserId == userId)
            .OrderByDescending(conversation => conversation.LastMessageAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        await _dbContext.Conversations.AddAsync(conversation, cancellationToken);
    }
}