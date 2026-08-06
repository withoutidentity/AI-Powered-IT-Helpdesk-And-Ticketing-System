using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.ToTable("knowledge_documents");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id).HasColumnName("id");
        builder.Property(document => document.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(document => document.SourceFile).HasColumnName("source_file").HasMaxLength(500).IsRequired();
        builder.Property(document => document.SourceType).HasColumnName("source_type").HasMaxLength(80).IsRequired();
        builder.Property(document => document.ContentHash).HasColumnName("content_hash").HasMaxLength(128).IsRequired();
        builder.Property(document => document.Status)
            .HasColumnName("status")
            .HasConversion(status => status.ToString(), value => Enum.Parse<KnowledgeDocumentStatus>(value))
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(document => document.FailureReason).HasColumnName("failure_reason").HasMaxLength(1000);
        builder.Property(document => document.UploadedAt).HasColumnName("uploaded_at").IsRequired();
        builder.Property(document => document.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(document => document.Status);
        builder.HasIndex(document => document.ContentHash);
        builder.HasIndex(document => document.SourceFile);
    }
}