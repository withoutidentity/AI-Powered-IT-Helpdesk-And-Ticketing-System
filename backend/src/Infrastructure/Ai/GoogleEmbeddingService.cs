using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Application.Common.Models;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Ai;

public sealed class GoogleEmbeddingService : IEmbeddingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly EmbeddingRequestRateLimiter _rateLimiter;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int? _dimensions;

    public string Model => _model;
    public int? Dimensions => _dimensions;

    public GoogleEmbeddingService(HttpClient httpClient, IConfiguration configuration, EmbeddingRequestRateLimiter rateLimiter)
    {
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _apiKey = configuration["GoogleAI:ApiKey"] ?? string.Empty;
        _model = configuration["Embedding:Model"] ?? "gemini-embedding-001";
        _dimensions = configuration.GetValue<int?>("Embedding:Dimensions");
    }

    public Task<EmbeddingResult> EmbedDocumentAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedAsync(text, "RETRIEVAL_DOCUMENT", cancellationToken);
    }

    public Task<EmbeddingResult> EmbedQueryAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedAsync(text, "RETRIEVAL_QUERY", cancellationToken);
    }

    private async Task<EmbeddingResult> EmbedAsync(string input, string taskType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Google AI Studio API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_model))
        {
            throw new InvalidOperationException("Embedding model is not configured.");
        }

        await _rateLimiter.WaitForSlotAsync(cancellationToken);

        var modelPath = _model.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
            ? _model
            : "models/" + _model;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/{modelPath}:embedContent");
        request.Headers.Add("x-goog-api-key", _apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new EmbedContentRequest(
                modelPath,
                new Content([new Part(input)]),
                taskType,
                _dimensions,
                new EmbedContentConfig(taskType, _dimensions)), JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Embedding provider returned {(int)response.StatusCode}: {body}");
        }

        var payload = JsonSerializer.Deserialize<EmbedContentResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Embedding provider returned an empty response.");
        var values = payload.Embedding?.Values ?? [];
        if (values.Count == 0)
        {
            throw new InvalidOperationException("Embedding provider returned an empty vector.");
        }

        if (_dimensions is > 0 && values.Count != _dimensions.Value)
        {
            throw new InvalidOperationException($"Embedding provider returned {values.Count} dimensions, expected {_dimensions.Value}.");
        }

        var normalizedValues = _dimensions is > 0 && _dimensions.Value != 3072
            ? Normalize(values)
            : values;

        return new EmbeddingResult(normalizedValues, _model, normalizedValues.Count);
    }

    private static IReadOnlyList<float> Normalize(IReadOnlyList<float> values)
    {
        var magnitude = Math.Sqrt(values.Sum(value => value * value));
        if (magnitude <= 0)
        {
            return values;
        }

        return values.Select(value => (float)(value / magnitude)).ToList();
    }

    private sealed record EmbedContentRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("content")] Content Content,
        [property: JsonPropertyName("taskType")] string TaskType,
        [property: JsonPropertyName("outputDimensionality")] int? OutputDimensionality,
        [property: JsonPropertyName("embedContentConfig")] EmbedContentConfig Config);

    private sealed record Content(
        [property: JsonPropertyName("parts")] IReadOnlyList<Part> Parts);

    private sealed record Part(
        [property: JsonPropertyName("text")] string Text);

    private sealed record EmbedContentConfig(
        [property: JsonPropertyName("taskType")] string TaskType,
        [property: JsonPropertyName("outputDimensionality")] int? OutputDimensionality);

    private sealed record EmbedContentResponse(
        [property: JsonPropertyName("embedding")] ContentEmbedding? Embedding);

    private sealed record ContentEmbedding(
        [property: JsonPropertyName("values")] IReadOnlyList<float> Values);
}


