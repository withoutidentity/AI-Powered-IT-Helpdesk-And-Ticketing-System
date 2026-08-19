namespace Application.KnowledgeBase.Models;

public sealed record ReindexKnowledgeDocumentResultDto(
    Guid DocumentId,
    string Status,
    int EmbeddedChunkCount,
    string EmbeddingModel,
    int EmbeddingDimensions,
    DateTimeOffset UpdatedAt);
