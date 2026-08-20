using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface IEmbeddingService
{
    string Model { get; }
    int? Dimensions { get; }
    Task<EmbeddingResult> EmbedDocumentAsync(string text, CancellationToken cancellationToken);
    Task<EmbeddingResult> EmbedQueryAsync(string text, CancellationToken cancellationToken);
}
