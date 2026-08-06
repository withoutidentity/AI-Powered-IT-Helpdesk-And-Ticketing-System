using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface IKnowledgeDocumentChunker
{
    IReadOnlyList<KnowledgeDocumentChunk> Split(string content);
}