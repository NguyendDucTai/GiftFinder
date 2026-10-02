using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class ClickTrackingConfiguration : IEntityTypeConfiguration<ClickTracking>
{
    public void Configure(EntityTypeBuilder<ClickTracking> builder)
    {
        builder.ToTable("ClickTrackings");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.SubId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.SubId);

        builder.Property(c => c.TrackingUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.IpAddress)
            .HasMaxLength(50);

        builder.Property(c => c.UserAgent)
            .HasMaxLength(500);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => new { c.ProductId, c.ClickedAt });
    }
}
