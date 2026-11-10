using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class MerchantOrderItemConfiguration : IEntityTypeConfiguration<MerchantOrderItem>
{
    public void Configure(EntityTypeBuilder<MerchantOrderItem> builder)
    {
        builder.ToTable("MerchantOrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(i => i.ProductImageUrl)
            .HasColumnType("text");

        builder.Property(i => i.Variant)
            .HasMaxLength(100);

        builder.HasOne(i => i.MerchantOrder)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.MerchantOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

