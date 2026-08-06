namespace Application.KnowledgeBase.Models;

public sealed record DocumentChunkDto(
    Guid Id,
    Guid DocumentId,
    int ChunkIndex,
    string Content,
    string ContentHash,
    int? TokenCount,
    string? EmbeddingModel,
    int? EmbeddingDimensions,
    DateTimeOffset CreatedAt);