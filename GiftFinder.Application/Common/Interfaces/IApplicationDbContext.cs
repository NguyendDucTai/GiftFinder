using GiftFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Merchant> Merchants { get; }
    DbSet<Product> Products { get; }
    DbSet<PriceHistory> PriceHistories { get; }
    DbSet<Tag> Tags { get; }
    DbSet<ProductTag> ProductTags { get; }
    DbSet<Wishlist> Wishlists { get; }
    DbSet<RecommendationLog> RecommendationLogs { get; }
    DbSet<RecommendationRule> RecommendationRules { get; }
    DbSet<ClickTracking> ClickTrackings { get; }
    DbSet<AffiliateOrder> AffiliateOrders { get; }
    DbSet<ReminderDate> ReminderDates { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
