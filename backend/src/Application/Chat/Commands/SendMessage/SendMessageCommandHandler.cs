using Application.Chat.Models;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.KnowledgeBase.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Chat.Commands.SendMessage;

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<SendMessageResponse>>
{
    private const string FallbackAssistantResponse = "I could not find a matching knowledge-base article for this issue yet. Please add more detail or create a ticket so IT support can review it.";

    private readonly ICurrentUserService _currentUser;
    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;
    private readonly IKnowledgeBaseSearchService _knowledgeBaseSearch;
    private readonly IChatAiService _chatAi;
    private readonly IMessageSourceRepository _messageSources;
    private readonly IUnitOfWork _unitOfWork;

    public SendMessageCommandHandler(
        ICurrentUserService currentUser,
        IConversationRepository conversations,
        IMessageRepository messages,
        IKnowledgeBaseSearchService knowledgeBaseSearch,
        IChatAiService chatAi,
        IMessageSourceRepository messageSources,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _conversations = conversations;
        _messages = messages;
        _knowledgeBaseSearch = knowledgeBaseSearch;
        _chatAi = chatAi;
        _messageSources = messageSources;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SendMessageResponse>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversations.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return Result<SendMessageResponse>.Failure("ConversationNotFound", "Conversation was not found.");
        }

        if (conversation.UserId != _currentUser.UserId)
        {
            return Result<SendMessageResponse>.Failure("Forbidden", "You do not have access to this conversation.");
        }

        var now = DateTimeOffset.UtcNow;
        var userMessage = conversation.AddMessage(MessageSender.User, request.Content, null, now);
        var searchResult = await _knowledgeBaseSearch.SearchAsync(request.Content, 3, cancellationToken);
        var assistantResponse = await BuildAssistantResponseAsync(request.Content, searchResult, cancellationToken);
        var assistantMessage = conversation.AddMessage(MessageSender.Assistant, assistantResponse.Content, null, now.AddMilliseconds(1));

        await _messages.AddAsync(userMessage, cancellationToken);
        await _messages.AddAsync(assistantMessage, cancellationToken);

        var sourceRows = assistantResponse.SourceItems
            .Select(item => MessageSource.Create(assistantMessage.Id, item.ChunkId, now.AddMilliseconds(2)))
            .ToList();
        await _messageSources.AddRangeAsync(sourceRows, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SendMessageResponse>.Success(new SendMessageResponse(
            ToDto(userMessage, []),
            ToDto(assistantMessage, ToSourceLabels(assistantResponse.SourceItems))));
    }

    private async Task<AssistantResponse> BuildAssistantResponseAsync(string question, Result<KnowledgeBaseSearchResultDto> searchResult, CancellationToken cancellationToken)
    {
        if (!HasSearchItems(searchResult))
        {
            return new AssistantResponse(FallbackAssistantResponse, []);
        }

        var items = OrderedSearchItems(searchResult.Value!);
        var sources = items
            .Select(item => new GroundingSource(item.DocumentTitle, item.ChunkIndex, item.Content))
            .ToList();

        try
        {
            var answer = await _chatAi.GenerateGroundedAnswerAsync(question, sources, cancellationToken);
            return new AssistantResponse(AppendSources(CleanPlainTextAnswer(answer), items), items);
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            return new AssistantResponse(BuildRetrievalOnlyResponse(items), items);
        }
    }

    private static bool HasSearchItems(Result<KnowledgeBaseSearchResultDto> searchResult)
    {
        return searchResult.IsSuccess && searchResult.Value is not null && searchResult.Value.Items.Count > 0;
    }

    private static IReadOnlyList<KnowledgeBaseSearchResultItemDto> OrderedSearchItems(KnowledgeBaseSearchResultDto searchResult)
    {
        return searchResult.Items
            .Take(3)
            .OrderBy(item => IsEscalationChunk(item) ? 1 : 0)
            .ThenBy(item => item.Rank)
            .ToList();
    }

    private static string CleanPlainTextAnswer(string answer)
    {
        return answer
            .Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("__", string.Empty, StringComparison.Ordinal)
            .Replace("### ", string.Empty, StringComparison.Ordinal)
            .Replace("## ", string.Empty, StringComparison.Ordinal)
            .Replace("# ", string.Empty, StringComparison.Ordinal)
            .Replace("`", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static string BuildRetrievalOnlyResponse(IReadOnlyList<KnowledgeBaseSearchResultItemDto> items)
    {
        var lines = new List<string>
        {
            "I found related knowledge-base guidance for this issue, but the AI answer service is unavailable right now.",
            string.Empty,
            "Suggested next steps:",
        };

        for (var index = 0; index < items.Count; index++)
        {
            lines.Add($"{index + 1}. {items[index].Preview}");
        }

        lines.Add(string.Empty);
        lines.Add(BuildSourcesBlock(items));

        return string.Join(Environment.NewLine, lines);
    }

    private static string AppendSources(string answer, IReadOnlyList<KnowledgeBaseSearchResultItemDto> items)
    {
        return answer.Trim() + Environment.NewLine + Environment.NewLine + BuildSourcesBlock(items);
    }

    private static string BuildSourcesBlock(IReadOnlyList<KnowledgeBaseSearchResultItemDto> items)
    {
        var lines = new List<string> { "Sources:" };
        foreach (var item in items)
        {
            lines.Add($"- {item.DocumentTitle}, chunk {item.ChunkIndex}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool IsEscalationChunk(KnowledgeBaseSearchResultItemDto item)
    {
        return item.Preview.Contains("escalation", StringComparison.OrdinalIgnoreCase)
            || item.Preview.Contains("contact IT", StringComparison.OrdinalIgnoreCase)
            || item.Preview.Contains("create ticket", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> ToSourceLabels(IReadOnlyList<KnowledgeBaseSearchResultItemDto> items)
    {
        return items.Select(item => $"{item.DocumentTitle}#chunk-{item.ChunkIndex}").ToList();
    }

    private sealed record AssistantResponse(string Content, IReadOnlyList<KnowledgeBaseSearchResultItemDto> SourceItems);

    private static MessageDto ToDto(Domain.Entities.Message message, IReadOnlyList<string> sourceDocuments)
    {
        return new MessageDto(
            message.Id,
            message.ConversationId,
            message.Sender.ToString(),
            message.Content,
            message.Intent?.ToString(),
            message.CreatedAt,
            sourceDocuments);
    }
}