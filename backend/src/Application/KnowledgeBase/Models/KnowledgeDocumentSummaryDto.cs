namespace Application.KnowledgeBase.Models;

public sealed record KnowledgeDocumentSummaryDto(
    Guid Id,
    string Title,
    string SourceFile,
    string SourceType,
    string ContentHash,
    string Status,
    string? FailureReason,
    int ChunkCount,
    DateTimeOffset UploadedAt,
    DateTimeOffset UpdatedAt);