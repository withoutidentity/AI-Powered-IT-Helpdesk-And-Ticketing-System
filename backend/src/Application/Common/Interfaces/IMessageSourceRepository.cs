using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IMessageSourceRepository
{
    Task<IReadOnlyList<MessageSourceReference>> ListByMessageIdsAsync(IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken);
    Task AddRangeAsync(IReadOnlyCollection<MessageSource> sources, CancellationToken cancellationToken);
}