using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Id).HasColumnName("id");
        builder.Property(refreshToken => refreshToken.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(refreshToken => refreshToken.TokenHash).HasColumnName("token_hash").HasMaxLength(200).IsRequired();
        builder.Property(refreshToken => refreshToken.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(refreshToken => refreshToken.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(refreshToken => refreshToken.RevokedAt).HasColumnName("revoked_at");
        builder.Property(refreshToken => refreshToken.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(200);

        builder.HasIndex(refreshToken => refreshToken.TokenHash).IsUnique();
        builder.HasIndex(refreshToken => refreshToken.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}