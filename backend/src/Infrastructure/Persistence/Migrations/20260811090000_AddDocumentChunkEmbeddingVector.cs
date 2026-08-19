using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260811090000_AddDocumentChunkEmbeddingVector")]
    public partial class AddDocumentChunkEmbeddingVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
            migrationBuilder.Sql("ALTER TABLE document_chunks ADD COLUMN embedding vector(768) NULL;");
            migrationBuilder.Sql("CREATE INDEX IX_document_chunks_embedding_hnsw ON document_chunks USING hnsw (embedding vector_cosine_ops) WHERE embedding IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_document_chunks_embedding_hnsw;");
            migrationBuilder.Sql("ALTER TABLE document_chunks DROP COLUMN IF EXISTS embedding;");
        }
    }
}


