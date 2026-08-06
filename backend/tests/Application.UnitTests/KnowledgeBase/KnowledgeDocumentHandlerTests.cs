using Application.Common.Interfaces;
using Application.KnowledgeBase.Commands.CreateKnowledgeDocument;
using Application.KnowledgeBase.Queries.GetKnowledgeDocumentDetail;
using Application.KnowledgeBase.Queries.GetKnowledgeDocuments;
using Application.KnowledgeBase.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Application.UnitTests.KnowledgeBase;

public sealed class KnowledgeDocumentHandlerTests
{
    [Fact]
    public async Task GetKnowledgeDocuments_ITAgent_ReturnsDocumentsWithChunkCount()
    {
        var agentId = Guid.NewGuid();
        var document = CreateReadyDocument("Wi-Fi Guide");
        var chunks = new FakeDocumentChunkRepository(DocumentChunk.Create(document.Id, 0, "Connect to Wi-Fi", "hash", DateTimeOffset.UtcNow));
        var handler = new GetKnowledgeDocumentsQueryHandler(
            new FakeCurrentUserService(agentId, UserRole.ITAgent),
            new FakeKnowledgeDocumentRepository(document),
            chunks);

        var result = await handler.Handle(new GetKnowledgeDocumentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items[0].ChunkCount.Should().Be(1);
    }

    [Fact]
    public async Task GetKnowledgeDocuments_Employee_ReturnsForbidden()
    {
        var handler = new GetKnowledgeDocumentsQueryHandler(
            new FakeCurrentUserService(Guid.NewGuid(), UserRole.Employee),
            new FakeKnowledgeDocumentRepository(),
            new FakeDocumentChunkRepository());

        var result = await handler.Handle(new GetKnowledgeDocumentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
    }

    [Fact]
    public async Task GetKnowledgeDocumentDetail_ITAdmin_ReturnsOrderedChunks()
    {
        var document = CreateReadyDocument("VPN Guide");
        var second = DocumentChunk.Create(document.Id, 1, "Step 2", "hash-2", DateTimeOffset.UtcNow);
        var first = DocumentChunk.Create(document.Id, 0, "Step 1", "hash-1", DateTimeOffset.UtcNow);
        var handler = new GetKnowledgeDocumentDetailQueryHandler(
            new FakeCurrentUserService(Guid.NewGuid(), UserRole.ITAdmin),
            new FakeKnowledgeDocumentRepository(document),
            new FakeDocumentChunkRepository(second, first));

        var result = await handler.Handle(new GetKnowledgeDocumentDetailQuery(document.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Chunks.Select(chunk => chunk.ChunkIndex).Should().Equal(0, 1);
    }

    [Fact]
    public async Task GetKnowledgeDocumentDetail_MissingDocument_ReturnsNotFound()
    {
        var handler = new GetKnowledgeDocumentDetailQueryHandler(
            new FakeCurrentUserService(Guid.NewGuid(), UserRole.ITAdmin),
            new FakeKnowledgeDocumentRepository(),
            new FakeDocumentChunkRepository());

        var result = await handler.Handle(new GetKnowledgeDocumentDetailQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DocumentNotFound");
    }

    [Fact]
    public async Task CreateKnowledgeDocument_ITAdmin_SplitsMarkdownContentIntoChunks()
    {
        var documents = new FakeKnowledgeDocumentRepository();
        var chunks = new FakeDocumentChunkRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateKnowledgeDocumentCommandHandler(
            new FakeCurrentUserService(Guid.NewGuid(), UserRole.ITAdmin),
            documents,
            chunks,
            new MarkdownKnowledgeDocumentChunker(),
            unitOfWork);
        var content = "# Wi-Fi\nConnect to office Wi-Fi.\n\n# VPN\nInstall the VPN client and sign in.";

        var result = await handler.Handle(new CreateKnowledgeDocumentCommand(" Wi-Fi ", "wifi.md", "text/markdown", content), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Ready");
        result.Value.Chunks.Should().HaveCount(2);
        result.Value.Chunks.Select(chunk => chunk.ChunkIndex).Should().Equal(0, 1);
        result.Value.Chunks[0].Content.Should().StartWith("# Wi-Fi");
        result.Value.Chunks[1].Content.Should().StartWith("# VPN");
        documents.Items.Should().ContainSingle();
        chunks.Items.Should().HaveCount(2);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public void MarkdownKnowledgeDocumentChunker_MarkdownHeadings_SplitsIntoOrderedSections()
    {
        var chunker = new MarkdownKnowledgeDocumentChunker();
        var content = "# Wi-Fi\nConnect to Office Wi-Fi.\n\n## Troubleshooting\nForget the network profile.\n\n# VPN\nInstall the VPN client.";

        var chunks = chunker.Split(content);

        chunks.Should().HaveCount(3);
        chunks.Select(chunk => chunk.ChunkIndex).Should().Equal(0, 1, 2);
        chunks[0].Content.Should().StartWith("# Wi-Fi");
        chunks[1].Content.Should().StartWith("## Troubleshooting");
        chunks[2].Content.Should().StartWith("# VPN");
    }

    [Fact]
    public async Task CreateKnowledgeDocument_ITAgent_ReturnsForbidden()
    {
        var documents = new FakeKnowledgeDocumentRepository();
        var chunks = new FakeDocumentChunkRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateKnowledgeDocumentCommandHandler(
            new FakeCurrentUserService(Guid.NewGuid(), UserRole.ITAgent),
            documents,
            chunks,
            new MarkdownKnowledgeDocumentChunker(),
            unitOfWork);

        var result = await handler.Handle(new CreateKnowledgeDocumentCommand("Wi-Fi", "wifi.md", "text/markdown", "content"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
        documents.Items.Should().BeEmpty();
        chunks.Items.Should().BeEmpty();
        unitOfWork.SaveCalls.Should().Be(0);
    }

    private static KnowledgeDocument CreateReadyDocument(string title)
    {
        var document = KnowledgeDocument.Create(title, title + ".md", "text/markdown", Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow);
        document.MarkReady(DateTimeOffset.UtcNow);
        return document;
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId, UserRole role)
        {
            UserId = userId;
            Role = role;
        }

        public Guid UserId { get; }
        public UserRole Role { get; }
    }

    private sealed class FakeKnowledgeDocumentRepository : IKnowledgeDocumentRepository
    {
        public FakeKnowledgeDocumentRepository(params KnowledgeDocument[] documents)
        {
            Items.AddRange(documents);
        }

        public List<KnowledgeDocument> Items { get; } = new();

        public Task<KnowledgeDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(document => document.Id == id));
        }

        public Task<IReadOnlyList<KnowledgeDocument>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            IReadOnlyList<KnowledgeDocument> result = Items
                .OrderByDescending(document => document.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<int> CountAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.Count);
        }

        public Task AddAsync(KnowledgeDocument document, CancellationToken cancellationToken)
        {
            Items.Add(document);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDocumentChunkRepository : IDocumentChunkRepository
    {
        public FakeDocumentChunkRepository(params DocumentChunk[] chunks)
        {
            Items.AddRange(chunks);
        }

        public List<DocumentChunk> Items { get; } = new();

        public Task<IReadOnlyList<DocumentChunk>> ListByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken)
        {
            IReadOnlyList<DocumentChunk> result = Items.Where(chunk => chunk.DocumentId == documentId).ToList();
            return Task.FromResult(result);
        }

        public Task AddAsync(DocumentChunk chunk, CancellationToken cancellationToken)
        {
            Items.Add(chunk);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }
}
