using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence;

public class ThrivtsDbContext : DbContext, IApplicationDbContext
{
    public ThrivtsDbContext(DbContextOptions<ThrivtsDbContext> options) : base(options)
    {
    }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Buyer> Buyers => Set<Buyer>();
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<SellerInvite> SellerInvites => Set<SellerInvite>();
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<Influencer> Influencers => Set<Influencer>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<SellerResponse> SellerResponses => Set<SellerResponse>();
    public DbSet<RequirementSellerOffer> RequirementSellerOffers => Set<RequirementSellerOffer>();
    public DbSet<OfferRound> OfferRounds => Set<OfferRound>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<DealAllocation> DealAllocations => Set<DealAllocation>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<InfluencerCommission> InfluencerCommissions => Set<InfluencerCommission>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<MessageThread> MessageThreads => Set<MessageThread>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<ShippingRate> ShippingRates => Set<ShippingRate>();

    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<BlockedIp> BlockedIps => Set<BlockedIp>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<OpsChatMessage> OpsChatMessages => Set<OpsChatMessage>();
    public DbSet<OpsPresence> OpsPresences => Set<OpsPresence>();
    public DbSet<OpsTask> OpsTasks => Set<OpsTask>();
    public DbSet<PartnerApplication> PartnerApplications => Set<PartnerApplication>();
    public DbSet<PlatformFeeConfig> PlatformFeeConfigs => Set<PlatformFeeConfig>();
    public DbSet<PlatformFeeOverride> PlatformFeeOverrides => Set<PlatformFeeOverride>();
    public DbSet<PlatformStats> PlatformStats => Set<PlatformStats>();
    public DbSet<StockEntry> StockEntries => Set<StockEntry>();
    public DbSet<StockoutEntry> StockoutEntries => Set<StockoutEntry>();
    public DbSet<WashReceivedEntry> WashReceivedEntries => Set<WashReceivedEntry>();
    public DbSet<WashSentEntry> WashSentEntries => Set<WashSentEntry>();
    public DbSet<TranslationEntry> Translations => Set<TranslationEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ThrivtsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
