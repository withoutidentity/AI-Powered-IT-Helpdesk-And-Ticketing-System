using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class TicketActivityConfiguration : IEntityTypeConfiguration<TicketActivity>
{
    public void Configure(EntityTypeBuilder<TicketActivity> builder)
    {
        builder.ToTable("ticket_activities");

        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Id).HasColumnName("id");
        builder.Property(activity => activity.TicketId).HasColumnName("ticket_id").IsRequired();
        builder.Property(activity => activity.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(activity => activity.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        builder.Property(activity => activity.Field).HasColumnName("field").HasMaxLength(50);
        builder.Property(activity => activity.OldValue).HasColumnName("old_value").HasMaxLength(200);
        builder.Property(activity => activity.NewValue).HasColumnName("new_value").HasMaxLength(200);
        builder.Property(activity => activity.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(activity => new { activity.TicketId, activity.CreatedAt });

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(activity => activity.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(activity => activity.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
