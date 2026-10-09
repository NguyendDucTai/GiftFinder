using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class GiftPollItemConfiguration : IEntityTypeConfiguration<GiftPollItem>
{
    public void Configure(EntityTypeBuilder<GiftPollItem> builder)
    {
        builder.ToTable("GiftPollItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.VoteCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(i => i.GiftPoll)
            .WithMany(p => p.PollItems)
            .HasForeignKey(i => i.GiftPollId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.GiftPollId, i.ProductId });
    }
}
