namespace Application.KnowledgeBase.Models;

public sealed record KnowledgeDocumentDetailDto(
    Guid Id,
    string Title,
    string SourceFile,
    string SourceType,
    string ContentHash,
    string Status,
    string? FailureReason,
    DateTimeOffset UploadedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DocumentChunkDto> Chunks);