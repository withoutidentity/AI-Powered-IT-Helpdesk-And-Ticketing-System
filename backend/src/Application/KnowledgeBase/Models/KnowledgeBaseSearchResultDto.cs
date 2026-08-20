using Application.Common.Models;

namespace Application.KnowledgeBase.Models;

public sealed record KnowledgeBaseSearchResultDto(
    string Query,
    int Limit,
    IReadOnlyList<KnowledgeBaseSearchResultItemDto> Items);

public sealed record KnowledgeBaseSearchResultItemDto(
    Guid DocumentId,
    string DocumentTitle,
    string SourceFile,
    string SourceType,
    Guid ChunkId,
    int ChunkIndex,
    string Content,
    string Preview,
    string? EmbeddingModel,
    int? EmbeddingDimensions,
    int Rank);