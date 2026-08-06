namespace Domain.Entities;

public sealed class DocumentChunk
{
    private DocumentChunk()
    {
        Content = string.Empty;
        ContentHash = string.Empty;
    }

    private DocumentChunk(
        Guid id,
        Guid documentId,
        int chunkIndex,
        string content,
        string contentHash,
        int? tokenCount,
        string? embeddingModel,
        int? embeddingDimensions,
        DateTimeOffset createdAt)
    {
        Id = id;
        DocumentId = documentId;
        ChunkIndex = chunkIndex;
        Content = content;
        ContentHash = contentHash;
        TokenCount = tokenCount;
        EmbeddingModel = embeddingModel;
        EmbeddingDimensions = embeddingDimensions;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string Content { get; private set; }
    public string ContentHash { get; private set; }
    public int? TokenCount { get; private set; }
    public string? EmbeddingModel { get; private set; }
    public int? EmbeddingDimensions { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static DocumentChunk Create(
        Guid documentId,
        int chunkIndex,
        string content,
        string contentHash,
        DateTimeOffset createdAt,
        int? tokenCount = null,
        string? embeddingModel = null,
        int? embeddingDimensions = null)
    {
        if (chunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkIndex), "Chunk index cannot be negative.");
        }

        if (tokenCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenCount), "Token count cannot be negative.");
        }

        if (embeddingDimensions < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(embeddingDimensions), "Embedding dimensions cannot be negative.");
        }

        return new DocumentChunk(
            Guid.NewGuid(),
            documentId,
            chunkIndex,
            NormalizeRequired(content, nameof(content)),
            NormalizeRequired(contentHash, nameof(contentHash)),
            tokenCount,
            string.IsNullOrWhiteSpace(embeddingModel) ? null : embeddingModel.Trim(),
            embeddingDimensions,
            createdAt);
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        return value.Trim();
    }
}