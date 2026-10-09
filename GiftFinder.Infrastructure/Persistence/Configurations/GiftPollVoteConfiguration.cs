using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class GiftPollVoteConfiguration : IEntityTypeConfiguration<GiftPollVote>
{
    public void Configure(EntityTypeBuilder<GiftPollVote> builder)
    {
        builder.ToTable("GiftPollVotes");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.IpAddress)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.UserAgent)
            .HasMaxLength(500);

        builder.HasOne(v => v.GiftPoll)
            .WithMany()
            .HasForeignKey(v => v.GiftPollId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Product)
            .WithMany()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Chống spam: 1 IP chỉ được tính 1 phiếu bầu cho mỗi cuộc bình chọn
        builder.HasIndex(v => new { v.GiftPollId, v.IpAddress })
            .IsUnique();
    }
}
