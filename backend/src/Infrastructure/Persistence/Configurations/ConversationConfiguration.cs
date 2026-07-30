using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(conversation => conversation.Id);

        builder.Property(conversation => conversation.Id).HasColumnName("id");
        builder.Property(conversation => conversation.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(conversation => conversation.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(conversation => conversation.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(conversation => conversation.LastMessageAt).HasColumnName("last_message_at").IsRequired();

        builder.HasIndex(conversation => new { conversation.UserId, conversation.LastMessageAt });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(conversation => conversation.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}