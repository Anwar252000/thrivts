using Mapster;

namespace Thrivts.Application.Common.Mappings;

/// <summary>
/// Marker for Mapster's assembly scan (services.AddMapster / TypeAdapterConfig.GlobalSettings.Scan).
/// Add IRegister implementations per feature folder for anything that needs custom mapping rules —
/// role-scoped projections (BuyerDealDto, SellerDealDto, ...) should still be written by hand as
/// explicit Select()/Adapt() calls, never trusted to an automatic profile (see identity-hiding rule).
/// </summary>
public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
    }
}
