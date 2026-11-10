using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class MerchantOrderConfiguration : IEntityTypeConfiguration<MerchantOrder>
{
    public void Configure(EntityTypeBuilder<MerchantOrder> builder)
    {
        builder.ToTable("MerchantOrders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.ReceiverName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(o => o.ReceiverPhone)
            .IsRequired()
            .HasMaxLength(25);

        builder.Property(o => o.ReceiverProvince)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.ReceiverDistrict)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.ReceiverAddress)
            .IsRequired()
            .HasMaxLength(350);

        builder.Property(o => o.DeliveryNote)
            .HasMaxLength(500);

        builder.Property(o => o.GiftMessage)
            .HasMaxLength(500);

        builder.Property(o => o.PaymentMethod)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("COD");

        builder.Property(o => o.ShippingMethod)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(o => o.ShippingCarrier)
            .HasMaxLength(100);

        builder.Property(o => o.TrackingCode)
            .HasMaxLength(100);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(o => o.CancelReason)
            .HasMaxLength(500);

        builder.HasOne(o => o.Merchant)
            .WithMany()
            .HasForeignKey(o => o.MerchantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.BuyerUser)
            .WithMany()
            .HasForeignKey(o => o.BuyerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Metadata.FindNavigation(nameof(MerchantOrder.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => new { o.MerchantId, o.CreatedAt });
        builder.HasIndex(o => o.OrderCode);
    }
}

