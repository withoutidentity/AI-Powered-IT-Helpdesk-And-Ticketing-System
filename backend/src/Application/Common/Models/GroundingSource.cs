namespace Application.Common.Models;

public sealed record GroundingSource(
    string DocumentTitle,
    int ChunkIndex,
    string Content);