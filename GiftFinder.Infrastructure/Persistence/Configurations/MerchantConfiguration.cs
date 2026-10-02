using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.ToTable("Merchants");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ShopName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.ContactEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.ContactPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(m => m.BusinessLicense)
            .HasMaxLength(100);
    }
}
