using System.Linq.Expressions;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Common;
using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // 1. User & Merchant (UC-13, UC-14, UC-29 / UC-06, UC-28)
    public DbSet<User> Users => Set<User>();
    public DbSet<Merchant> Merchants => Set<Merchant>();

    // 2. Product Catalog & Tags (UC-05, UC-06, UC-07, UC-08, UC-18, UC-20, UC-28)
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ProductTag> ProductTags => Set<ProductTag>();

    // 3. Recommendation & User Engagement (UC-01, UC-02, UC-03, UC-04, UC-16, UC-31)
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<RecommendationLog> RecommendationLogs => Set<RecommendationLog>();
    public DbSet<RecommendationRule> RecommendationRules => Set<RecommendationRule>();

    // 4. Affiliate Tracking (UC-09, UC-10, UC-11, UC-12, UC-19)
    public DbSet<ClickTracking> ClickTrackings => Set<ClickTracking>();
    public DbSet<AffiliateOrder> AffiliateOrders => Set<AffiliateOrder>();

    // 5. Billing, Reminders & Notifications (UC-15, UC-17, UC-21, UC-22, UC-23, UC-24, UC-25, UC-26, UC-27)
    public DbSet<ReminderDate> ReminderDates => Set<ReminderDate>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Nạp tự động toàn bộ IEntityTypeConfiguration từ assembly
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global Query Filter: Tự động loại trừ các bản ghi IsDeleted == true cho các entity kế thừa BaseAuditableEntity (BR-08)
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseAuditableEntity.IsDeleted));
                var condition = Expression.Equal(property, Expression.Constant(false));
                var lambda = Expression.Lambda(condition, parameter);

                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override int SaveChanges()
    {
        ApplyAuditingAndSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditingAndSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditingAndSoftDelete()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.IsDeleted = false;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;

                case EntityState.Deleted:
                    // Soft-delete (BR-08): Chuyển thành trạng thái Modified với IsDeleted = true
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }
    }
}