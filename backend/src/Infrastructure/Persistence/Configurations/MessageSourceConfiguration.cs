using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class MessageSourceConfiguration : IEntityTypeConfiguration<MessageSource>
{
    public void Configure(EntityTypeBuilder<MessageSource> builder)
    {
        builder.ToTable("message_sources");

        builder.HasKey(source => source.Id);

        builder.Property(source => source.Id).HasColumnName("id");
        builder.Property(source => source.MessageId).HasColumnName("message_id").IsRequired();
        builder.Property(source => source.DocumentChunkId).HasColumnName("document_chunk_id").IsRequired();
        builder.Property(source => source.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(source => new { source.MessageId, source.DocumentChunkId }).IsUnique();
        builder.HasIndex(source => source.DocumentChunkId);

        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(source => source.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<DocumentChunk>()
            .WithMany()
            .HasForeignKey(source => source.DocumentChunkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}