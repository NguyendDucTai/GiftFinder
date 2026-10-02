using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(t => t.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.GatewayTransactionId)
            .HasMaxLength(100);

        builder.HasIndex(t => t.GatewayTransactionId)
            .HasFilter("\"GatewayTransactionId\" IS NOT NULL");

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.FeaturedPlan)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Product)
            .WithMany()
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}