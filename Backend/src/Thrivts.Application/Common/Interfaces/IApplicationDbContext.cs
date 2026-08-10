using Microsoft.EntityFrameworkCore;
using Thrivts.Domain.Entities;

namespace Thrivts.Application.Common.Interfaces;

/// <summary>
/// Application-layer view of the DbContext. Infrastructure's ThrivtsDbContext implements this,
/// so handlers can be unit-tested against a mock without referencing EF Core's runtime/Npgsql.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Profile> Profiles { get; }
    DbSet<Buyer> Buyers { get; }
    DbSet<Seller> Sellers { get; }
    DbSet<SellerInvite> SellerInvites { get; }
    DbSet<Agency> Agencies { get; }
    DbSet<Influencer> Influencers { get; }
    DbSet<Category> Categories { get; }
    DbSet<Requirement> Requirements { get; }
    DbSet<SellerResponse> SellerResponses { get; }
    DbSet<RequirementSellerOffer> RequirementSellerOffers { get; }
    DbSet<OfferRound> OfferRounds { get; }
    DbSet<Deal> Deals { get; }
    DbSet<DealAllocation> DealAllocations { get; }
    DbSet<Commission> Commissions { get; }
    DbSet<InfluencerCommission> InfluencerCommissions { get; }
    DbSet<Dispute> Disputes { get; }
    DbSet<MessageThread> MessageThreads { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ExchangeRate> ExchangeRates { get; }
    DbSet<ShippingRate> ShippingRates { get; }

    DbSet<AppConfig> AppConfigs { get; }
    DbSet<BlockedIp> BlockedIps { get; }
    DbSet<Message> Messages { get; }
    DbSet<OpsChatMessage> OpsChatMessages { get; }
    DbSet<OpsPresence> OpsPresences { get; }
    DbSet<OpsTask> OpsTasks { get; }
    DbSet<PartnerApplication> PartnerApplications { get; }
    DbSet<PlatformFeeConfig> PlatformFeeConfigs { get; }
    DbSet<PlatformFeeOverride> PlatformFeeOverrides { get; }
    DbSet<PlatformStats> PlatformStats { get; }
    DbSet<StockEntry> StockEntries { get; }
    DbSet<StockoutEntry> StockoutEntries { get; }
    DbSet<WashReceivedEntry> WashReceivedEntries { get; }
    DbSet<WashSentEntry> WashSentEntries { get; }
    DbSet<TranslationEntry> Translations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
