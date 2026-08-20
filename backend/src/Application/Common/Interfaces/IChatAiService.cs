using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface IChatAiService
{
    Task<string> GenerateGroundedAnswerAsync(string question, IReadOnlyList<GroundingSource> sources, CancellationToken cancellationToken);
}