using ErrorOr;
using Mediator;

namespace Thrivts.Application.Admin.Sellers;

/// <summary>Replaces verify_seller_kyc / unverify_seller_kyc (see fixes_applied/12_FIX_unverify_kyc.sql).</summary>
public sealed record SetSellerKycVerificationCommand(Guid SellerId, bool Verified) : ICommand<ErrorOr<Success>>;
