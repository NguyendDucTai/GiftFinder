using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(p => p.Price)
            .IsRequired();

        builder.Property(p => p.OriginalUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(p => p.AffiliateUrl)
            .HasMaxLength(1000);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(p => p.RejectReason)
            .HasMaxLength(500);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.Source)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(p => new { p.Source, p.ExternalProductId })
            .IsUnique()
            .HasFilter("\"ExternalProductId\" IS NOT NULL");

        builder.HasIndex(p => new { p.Status, p.Price });
        builder.HasIndex(p => new { p.IsFeatured, p.FeaturedUntil });

        builder.HasOne(p => p.Merchant)
            .WithMany()
            .HasForeignKey(p => p.MerchantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Metadata.FindNavigation(nameof(Product.ProductTags))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Product.PriceHistories))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}