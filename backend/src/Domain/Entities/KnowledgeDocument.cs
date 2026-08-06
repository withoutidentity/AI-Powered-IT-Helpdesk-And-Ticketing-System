using Domain.Enums;

namespace Domain.Entities;

public sealed class KnowledgeDocument
{
    private KnowledgeDocument()
    {
        Title = string.Empty;
        SourceFile = string.Empty;
        SourceType = string.Empty;
        ContentHash = string.Empty;
    }

    private KnowledgeDocument(
        Guid id,
        string title,
        string sourceFile,
        string sourceType,
        string contentHash,
        DateTimeOffset uploadedAt)
    {
        Id = id;
        Title = title;
        SourceFile = sourceFile;
        SourceType = sourceType;
        ContentHash = contentHash;
        Status = KnowledgeDocumentStatus.Processing;
        UploadedAt = uploadedAt;
        UpdatedAt = uploadedAt;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string SourceFile { get; private set; }
    public string SourceType { get; private set; }
    public string ContentHash { get; private set; }
    public KnowledgeDocumentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static KnowledgeDocument Create(
        string title,
        string sourceFile,
        string sourceType,
        string contentHash,
        DateTimeOffset uploadedAt)
    {
        return new KnowledgeDocument(
            Guid.NewGuid(),
            NormalizeRequired(title, nameof(title)),
            NormalizeRequired(sourceFile, nameof(sourceFile)),
            NormalizeRequired(sourceType, nameof(sourceType)),
            NormalizeRequired(contentHash, nameof(contentHash)),
            uploadedAt);
    }

    public void MarkReady(DateTimeOffset updatedAt)
    {
        Status = KnowledgeDocumentStatus.Ready;
        FailureReason = null;
        UpdatedAt = updatedAt;
    }

    public void MarkFailed(string failureReason, DateTimeOffset updatedAt)
    {
        Status = KnowledgeDocumentStatus.Failed;
        FailureReason = NormalizeRequired(failureReason, nameof(failureReason));
        UpdatedAt = updatedAt;
    }

    public void MarkProcessing(DateTimeOffset updatedAt)
    {
        Status = KnowledgeDocumentStatus.Processing;
        FailureReason = null;
        UpdatedAt = updatedAt;
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