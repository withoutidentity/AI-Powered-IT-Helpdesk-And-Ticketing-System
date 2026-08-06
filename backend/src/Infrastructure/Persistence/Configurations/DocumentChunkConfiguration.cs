using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.Id).HasColumnName("id");
        builder.Property(chunk => chunk.DocumentId).HasColumnName("document_id").IsRequired();
        builder.Property(chunk => chunk.ChunkIndex).HasColumnName("chunk_index").IsRequired();
        builder.Property(chunk => chunk.Content).HasColumnName("content").IsRequired();
        builder.Property(chunk => chunk.ContentHash).HasColumnName("content_hash").HasMaxLength(128).IsRequired();
        builder.Property(chunk => chunk.TokenCount).HasColumnName("token_count");
        builder.Property(chunk => chunk.EmbeddingModel).HasColumnName("embedding_model").HasMaxLength(120);
        builder.Property(chunk => chunk.EmbeddingDimensions).HasColumnName("embedding_dimensions");
        builder.Property(chunk => chunk.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.ChunkIndex }).IsUnique();
        builder.HasIndex(chunk => chunk.ContentHash);

        builder.HasOne<KnowledgeDocument>()
            .WithMany()
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}