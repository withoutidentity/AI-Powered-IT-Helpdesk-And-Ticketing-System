using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Application.Common.Models;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Ai;

public sealed class GroqEmbeddingService : IEmbeddingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int? _expectedDimensions;

    public GroqEmbeddingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Groq:ApiKey"] ?? string.Empty;
        _model = configuration["Groq:EmbeddingModel"] ?? string.Empty;
        _expectedDimensions = configuration.GetValue<int?>("Embedding:Dimensions");
    }

    public Task<EmbeddingResult> EmbedDocumentAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedAsync("search_document: " + text, cancellationToken);
    }

    public Task<EmbeddingResult> EmbedQueryAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedAsync("search_query: " + text, cancellationToken);
    }

    private async Task<EmbeddingResult> EmbedAsync(string input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Groq API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_model))
        {
            throw new InvalidOperationException("Groq embedding model is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new EmbeddingRequest(_model, input), JsonOptions),
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

        if (_expectedDimensions is > 0 && values.Count != _expectedDimensions.Value)
        {
            throw new InvalidOperationException($"Embedding provider returned {values.Count} dimensions, expected {_expectedDimensions.Value}.");
        }

        return new EmbeddingResult(values, payload.Model ?? _model, values.Count);
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("data")] IReadOnlyList<EmbeddingData> Data);

    private sealed record EmbeddingData(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] IReadOnlyList<float> Embedding);
}
