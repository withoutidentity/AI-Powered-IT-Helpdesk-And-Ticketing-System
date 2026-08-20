using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Application.Common.Models;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Ai;

public sealed class OpenAiEmbeddingService : IEmbeddingService
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

    public OpenAiEmbeddingService(HttpClient httpClient, IConfiguration configuration, EmbeddingRequestRateLimiter rateLimiter)
    {
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
        _model = configuration["Embedding:Model"] ?? "text-embedding-3-small";
        _dimensions = configuration.GetValue<int?>("Embedding:Dimensions");
    }

    public Task<EmbeddingResult> EmbedDocumentAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedAsync(text, cancellationToken);
    }

    public Task<EmbeddingResult> EmbedQueryAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedAsync(text, cancellationToken);
    }

    private async Task<EmbeddingResult> EmbedAsync(string input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_model))
        {
            throw new InvalidOperationException("Embedding model is not configured.");
        }

        await _rateLimiter.WaitForSlotAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new EmbeddingRequest(_model, input, _dimensions), JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Embedding provider returned {(int)response.StatusCode}: {body}");
        }

        var payload = JsonSerializer.Deserialize<EmbeddingResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Embedding provider returned an empty response.");
        var item = payload.Data.OrderBy(data => data.Index).FirstOrDefault()
            ?? throw new InvalidOperationException("Embedding provider returned no embedding data.");
        var values = item.Embedding;
        if (values.Count == 0)
        {
            throw new InvalidOperationException("Embedding provider returned an empty vector.");
        }

        if (_dimensions is > 0 && values.Count != _dimensions.Value)
        {
            throw new InvalidOperationException($"Embedding provider returned {values.Count} dimensions, expected {_dimensions.Value}.");
        }

        return new EmbeddingResult(values, payload.Model ?? _model, values.Count);
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("dimensions")] int? Dimensions);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("data")] IReadOnlyList<EmbeddingData> Data);

    private sealed record EmbeddingData(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] IReadOnlyList<float> Embedding);
}

