namespace Application.Chat.Models;

public sealed record ConversationDto(Guid Id, Guid UserId, string Title, DateTimeOffset CreatedAt, DateTimeOffset LastMessageAt);