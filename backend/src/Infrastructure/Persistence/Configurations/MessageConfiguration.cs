using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id).HasColumnName("id");
        builder.Property(message => message.ConversationId).HasColumnName("conversation_id").IsRequired();
        builder.Property(message => message.Sender)
            .HasColumnName("sender")
            .HasConversion(sender => sender.ToString(), value => Enum.Parse<MessageSender>(value))
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(message => message.Content).HasColumnName("content").IsRequired();
        builder.Property(message => message.Intent)
            .HasColumnName("intent")
            .HasConversion(intent => intent == null ? null : intent.ToString(), value => value == null ? null : Enum.Parse<MessageIntent>(value))
            .HasMaxLength(30);
        builder.Property(message => message.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(message => new { message.ConversationId, message.CreatedAt });

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}