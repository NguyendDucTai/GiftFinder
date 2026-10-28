using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class AffiliateOrderConfiguration : IEntityTypeConfiguration<AffiliateOrder>
{
    public void Configure(EntityTypeBuilder<AffiliateOrder> builder)
    {
        builder.ToTable("AffiliateOrders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Source)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(o => o.ExternalOrderId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(o => new { o.Source, o.ExternalOrderId })
            .IsUnique();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasOne(o => o.ClickTracking)
            .WithMany()
            .HasForeignKey(o => o.ClickTrackingId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
