namespace Application.Chat.Models;

public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    string Sender,
    string Content,
    string? Intent,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> SourceDocuments);