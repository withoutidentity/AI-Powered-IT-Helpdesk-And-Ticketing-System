namespace Application.Common.Models;

public sealed record EmbeddingResult(
    IReadOnlyList<float> Values,
    string Model,
    int Dimensions);
