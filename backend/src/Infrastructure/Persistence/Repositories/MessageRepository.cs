using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _dbContext;

    public MessageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Messages.FirstOrDefaultAsync(message => message.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> ListByConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await _dbContext.Messages
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken)
    {
        await _dbContext.Messages.AddAsync(message, cancellationToken);
    }
}