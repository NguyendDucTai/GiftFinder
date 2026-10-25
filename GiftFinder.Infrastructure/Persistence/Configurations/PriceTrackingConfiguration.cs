using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class PriceTrackingConfiguration : IEntityTypeConfiguration<PriceTracking>
{
    public void Configure(EntityTypeBuilder<PriceTracking> builder)
    {
        builder.ToTable("PriceTrackings");

        builder.HasKey(pt => pt.Id);

        builder.HasIndex(pt => pt.ProductId);

        builder.HasIndex(pt => new { pt.UserId, pt.ProductId });

        builder.Property(pt => pt.InitialPrice)
            .IsRequired();

        builder.HasOne(pt => pt.User)
            .WithMany()
            .HasForeignKey(pt => pt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.Product)
            .WithMany()
            .HasForeignKey(pt => pt.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
