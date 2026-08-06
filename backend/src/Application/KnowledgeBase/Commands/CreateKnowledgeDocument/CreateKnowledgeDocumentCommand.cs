using Application.Common.Models;
using Application.KnowledgeBase.Models;
using MediatR;

namespace Application.KnowledgeBase.Commands.CreateKnowledgeDocument;

public sealed record CreateKnowledgeDocumentCommand(
    string Title,
    string SourceFile,
    string SourceType,
    string Content) : IRequest<Result<KnowledgeDocumentDetailDto>>;