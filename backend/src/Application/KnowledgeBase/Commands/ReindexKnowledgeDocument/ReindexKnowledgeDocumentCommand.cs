using Application.Common.Models;
using Application.KnowledgeBase.Models;
using MediatR;

namespace Application.KnowledgeBase.Commands.ReindexKnowledgeDocument;

public sealed record ReindexKnowledgeDocumentCommand(Guid DocumentId) : IRequest<Result<ReindexKnowledgeDocumentResultDto>>;

