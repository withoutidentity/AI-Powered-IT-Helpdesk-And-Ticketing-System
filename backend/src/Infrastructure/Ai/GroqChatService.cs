using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Application.Common.Models;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Ai;

public sealed class GroqChatService : IChatAiService
{
    private const int MaxContextCharacters = 6000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public GroqChatService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Groq:ApiKey"] ?? string.Empty;
        _model = configuration["Groq:ChatModel"] ?? "openai/gpt-oss-120b";
    }

    public async Task<string> GenerateGroundedAnswerAsync(string question, IReadOnlyList<GroundingSource> sources, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Groq API key is not configured.");
        }

        if (sources.Count == 0)
        {
            throw new InvalidOperationException("At least one grounding source is required.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(new ChatCompletionRequest(
            _model,
            [
                new ChatMessage("system", BuildSystemPrompt()),
                new ChatMessage("user", BuildUserPrompt(question, sources))
            ],
            0.2,
            700));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Chat provider returned {(int)response.StatusCode}: {body}");
        }

        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Chat provider returned an empty response.");

        var answer = completion.Choices.FirstOrDefault()?.Message.Content?.Trim();
        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException("Chat provider returned an empty answer.");
        }

        return answer;
    }

    private static string BuildSystemPrompt()
    {
        return "You are an IT Helpdesk assistant. Answer only from the provided knowledge-base context. "
            + "If the context is insufficient, say that the knowledge base does not contain enough information and suggest creating a ticket. "
            + "Do not guess. Do not invent policies, URLs, passwords, phone numbers, or system names. "
            + "Respond in the same language as the user's question when practical. Summarize as clear numbered steps. Use plain text only: do not use Markdown formatting, bold markers, headings, tables, blockquotes, or code fences.";
    }

    private static string BuildUserPrompt(string question, IReadOnlyList<GroundingSource> sources)
    {
        var context = string.Join("\n\n", sources.Select((source, index) =>
            $"Source {index + 1}: {source.DocumentTitle}, chunk {source.ChunkIndex}\n{source.Content}"));

        if (context.Length > MaxContextCharacters)
        {
            context = context[..MaxContextCharacters];
        }

        return $"Knowledge-base context:\n{context}\n\nUser question:\n{question.Trim()}\n\nAnswer:";
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(
        [property: JsonPropertyName("message")] ChatMessage Message);
}