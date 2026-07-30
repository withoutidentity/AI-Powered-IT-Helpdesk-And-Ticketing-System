namespace Application.Chat.Models;

public sealed record SendMessageResponse(MessageDto UserMessage, MessageDto AssistantMessage);