using System.Text.RegularExpressions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.KnowledgeBase.Models;
using Domain.Entities;

namespace Application.KnowledgeBase.Services;

public sealed class KnowledgeBaseSearchService : IKnowledgeBaseSearchService
{
    private const int PreviewLength = 220;

    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentChunkRepository _chunks;
    private readonly IKnowledgeDocumentRepository _documents;

    public KnowledgeBaseSearchService(
        IEmbeddingService embeddingService,
        IDocumentChunkRepository chunks,
        IKnowledgeDocumentRepository documents)
    {
        _embeddingService = embeddingService;
        _chunks = chunks;
        _documents = documents;
    }

    public async Task<Result<KnowledgeBaseSearchResultDto>> SearchAsync(string query, int limit, CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim();
        if (normalizedQuery.Length == 0)
        {
            return Result<KnowledgeBaseSearchResultDto>.Failure("ValidationFailed", "Search query is required.");
        }

        try
        {
            var queryEmbedding = await _embeddingService.EmbedQueryAsync(normalizedQuery, cancellationToken);
            if (queryEmbedding.Values.Count == 0)
            {
                return Result<KnowledgeBaseSearchResultDto>.Failure("EmbeddingFailed", "Embedding provider returned an empty query vector.");
            }

            if (queryEmbedding.Values.Count != queryEmbedding.Dimensions)
            {
                return Result<KnowledgeBaseSearchResultDto>.Failure("EmbeddingFailed", "Embedding provider returned a vector length that does not match its dimensions.");
            }

            var chunks = await _chunks.SearchSimilarAsync(queryEmbedding.Values, limit, cancellationToken);
            var documentIds = chunks.Select(chunk => chunk.DocumentId).Distinct().ToArray();
            var documents = await _documents.ListByIdsAsync(documentIds, cancellationToken);
            var documentsById = documents.ToDictionary(document => document.Id);

            var items = chunks
                .Select((chunk, index) => ToItem(chunk, documentsById, index + 1))
                .Where(item => item is not null)
                .Select(item => item!)
                .ToList();

            return Result<KnowledgeBaseSearchResultDto>.Success(new KnowledgeBaseSearchResultDto(normalizedQuery, limit, items));
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            return Result<KnowledgeBaseSearchResultDto>.Failure("EmbeddingFailed", exception.Message);
        }
    }

    private static KnowledgeBaseSearchResultItemDto? ToItem(DocumentChunk chunk, IReadOnlyDictionary<Guid, KnowledgeDocument> documentsById, int rank)
    {
        if (!documentsById.TryGetValue(chunk.DocumentId, out var document))
        {
            return null;
        }

        return new KnowledgeBaseSearchResultItemDto(
            document.Id,
            document.Title,
            document.SourceFile,
            document.SourceType,
            chunk.Id,
            chunk.ChunkIndex,
            chunk.Content,
            ToPreview(chunk.Content),
            chunk.EmbeddingModel,
            chunk.EmbeddingDimensions,
            rank);
    }

    private static string ToPreview(string content)
    {
        var cleaned = CleanMarkdown(content);
        return cleaned.Length <= PreviewLength ? cleaned : cleaned[..PreviewLength] + "...";
    }

    private static string CleanMarkdown(string content)
    {
        var cleaned = content.Replace("\\.", ".", StringComparison.Ordinal);
        cleaned = Regex.Replace(cleaned, @"^\s{0,3}#{1,6}\s*", string.Empty, RegexOptions.Multiline);
        cleaned = Regex.Replace(cleaned, @"\*\*(.*?)\*\*", "$1");
        cleaned = Regex.Replace(cleaned, @"__(.*?)__", "$1");
        cleaned = Regex.Replace(cleaned, @"`([^`]*)`", "$1");
        cleaned = Regex.Replace(cleaned, @"^\s{0,3}>\s?", string.Empty, RegexOptions.Multiline);
        cleaned = Regex.Replace(cleaned, @"^\s*[-*+]\s+", string.Empty, RegexOptions.Multiline);
        cleaned = Regex.Replace(cleaned, @"^\s*\d+[.)]\s+", string.Empty, RegexOptions.Multiline);
        cleaned = Regex.Replace(cleaned, @"\[(.*?)\]\((.*?)\)", "$1");
        cleaned = cleaned.Replace("|", " ", StringComparison.Ordinal);
        return string.Join(' ', cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}