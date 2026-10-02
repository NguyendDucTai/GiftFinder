using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class ReminderDateConfiguration : IEntityTypeConfiguration<ReminderDate>
{
    public void Configure(EntityTypeBuilder<ReminderDate> builder)
    {
        builder.ToTable("ReminderDates");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.RecipientRelation)
            .HasMaxLength(100);

        builder.Property(r => r.Note)
            .HasMaxLength(500);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
