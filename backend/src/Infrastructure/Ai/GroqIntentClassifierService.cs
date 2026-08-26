using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Ai;

public sealed class GroqIntentClassifierService : IIntentClassifierService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public GroqIntentClassifierService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Groq:ApiKey"] ?? string.Empty;
        _model = configuration["Groq:ChatModel"] ?? "openai/gpt-oss-120b";
    }

    public async Task<MessageIntent> ClassifyAsync(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Groq API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(new ChatCompletionRequest(
            _model,
            [
                new ChatMessage("system", "Classify the user's IT helpdesk message. Return JSON only in exactly this shape: {\"intent\":\"Greeting|Question|Action\"}. Greeting is a social greeting. Question asks for information or troubleshooting. Action reports a problem or requests human intervention, repair, access, or follow-up. Do not add any other text."),
                new ChatMessage("user", message.Trim())
            ],
            0,
            30,
            new ResponseFormat("json_object")));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Intent provider returned {(int)response.StatusCode}: {body}");
        }

        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Intent provider returned an empty response.");
        var content = completion.Choices.FirstOrDefault()?.Message.Content;
        var result = string.IsNullOrWhiteSpace(content)
            ? null
            : JsonSerializer.Deserialize<IntentResponse>(content, JsonOptions);

        return Enum.TryParse<MessageIntent>(result?.Intent, true, out var intent)
            ? intent
            : throw new InvalidOperationException("Intent provider returned an invalid intent.");
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("response_format")] ResponseFormat ResponseFormat);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ResponseFormat([property: JsonPropertyName("type")] string Type);
    private sealed record IntentResponse([property: JsonPropertyName("intent")] string? Intent);
    private sealed record ChatCompletionResponse([property: JsonPropertyName("choices")] IReadOnlyList<ChatChoice> Choices);
    private sealed record ChatChoice([property: JsonPropertyName("message")] ChatMessage Message);
}
