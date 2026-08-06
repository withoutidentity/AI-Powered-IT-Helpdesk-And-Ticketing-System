namespace Application.Common.Models;

public sealed record KnowledgeDocumentChunk(
    int ChunkIndex,
    string Content,
    int TokenCount);