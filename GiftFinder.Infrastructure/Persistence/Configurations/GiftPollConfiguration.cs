using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class GiftPollConfiguration : IEntityTypeConfiguration<GiftPoll>
{
    public void Configure(EntityTypeBuilder<GiftPoll> builder)
    {
        builder.ToTable("GiftPolls");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.CreatorName)
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.ShareCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(p => p.ShareCode)
            .IsUnique();

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PollItems)
            .WithOne(i => i.GiftPoll)
            .HasForeignKey(i => i.GiftPollId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
