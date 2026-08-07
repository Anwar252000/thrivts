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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ThrivtsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
