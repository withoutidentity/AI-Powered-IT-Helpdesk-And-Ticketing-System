using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("ticket_comments");

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Id).HasColumnName("id");
        builder.Property(comment => comment.TicketId).HasColumnName("ticket_id").IsRequired();
        builder.Property(comment => comment.AuthorId).HasColumnName("author_id").IsRequired();
        builder.Property(comment => comment.Content).HasColumnName("content").IsRequired();
        builder.Property(comment => comment.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(comment => new { comment.TicketId, comment.CreatedAt });

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(comment => comment.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(comment => comment.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}