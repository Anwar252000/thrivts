using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per buyer. Id == the buyer's auth user id (same convention as Profile/Seller).
/// Unlike Seller, a buyer's identity is not hidden from the platform — only from other
/// sellers/buyers on the board. NOTE: CompanyName/Country are inferred from the buyer portal's
/// use of "company" in admin screens; verify exact column set once the schema is exported.
/// </summary>
public class Buyer : BaseEntity, IAggregateRoot
{
    public string? CompanyName { get; private set; }
    public string? Country { get; private set; }

    private Buyer()
    {
        // EF Core
    }

    public Buyer(Guid authUserId, string? companyName = null, string? country = null)
    {
        Id = authUserId;
        CompanyName = companyName;
        Country = country;
    }

    public void UpdateProfile(string? companyName, string? country)
    {
        CompanyName = companyName;
        Country = country;
    }
}
