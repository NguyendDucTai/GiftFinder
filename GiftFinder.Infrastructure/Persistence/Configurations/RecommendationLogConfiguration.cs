using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class RecommendationLogConfiguration : IEntityTypeConfiguration<RecommendationLog>
{
    public void Configure(EntityTypeBuilder<RecommendationLog> builder)
    {
        builder.ToTable("RecommendationLogs");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.SearchText).HasMaxLength(250);
        builder.Property(r => r.Recipient).HasMaxLength(100);
        builder.Property(r => r.Occasion).HasMaxLength(100);
        builder.Property(r => r.Hobbies).HasMaxLength(300);
        builder.Property(r => r.FeedbackNote).HasMaxLength(500);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}