namespace Application.Common.Models;

public sealed record MessageSourceReference(
    Guid MessageId,
    Guid DocumentChunkId,
    string DocumentTitle,
    int ChunkIndex);