using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftFinder.Infrastructure.Persistence.Configurations;

public class RecommendationRuleConfiguration : IEntityTypeConfiguration<RecommendationRule>
{
    public void Configure(EntityTypeBuilder<RecommendationRule> builder)
    {
        builder.ToTable("RecommendationRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Version)
            .IsRequired()
            .HasMaxLength(50);
    }
}