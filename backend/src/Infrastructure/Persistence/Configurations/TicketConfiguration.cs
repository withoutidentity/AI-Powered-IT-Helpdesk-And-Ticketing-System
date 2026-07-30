using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.Id).HasColumnName("id");
        builder.Property(ticket => ticket.ConversationId).HasColumnName("conversation_id").IsRequired();
        builder.Property(ticket => ticket.MessageId).HasColumnName("message_id");
        builder.Property(ticket => ticket.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(ticket => ticket.AssignedTo).HasColumnName("assigned_to");
        builder.Property(ticket => ticket.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(ticket => ticket.Description).HasColumnName("description").IsRequired();
        builder.Property(ticket => ticket.AttachmentsJson)
            .HasColumnName("attachments")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();
        builder.Property(ticket => ticket.Status)
            .HasColumnName("status")
            .HasConversion(status => status.ToString(), value => Enum.Parse<TicketStatus>(value))
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(ticket => ticket.Priority)
            .HasColumnName("priority")
            .HasConversion(priority => priority.ToString(), value => Enum.Parse<TicketPriority>(value))
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(ticket => ticket.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(ticket => ticket.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(ticket => new { ticket.CreatedBy, ticket.CreatedAt });
        builder.HasIndex(ticket => new { ticket.AssignedTo, ticket.Status });
        builder.HasIndex(ticket => ticket.Status);

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(ticket => ticket.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(ticket => ticket.MessageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ticket => ticket.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ticket => ticket.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);
    }
}