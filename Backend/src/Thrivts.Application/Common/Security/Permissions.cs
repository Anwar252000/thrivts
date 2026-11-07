using Thrivts.Domain.Identity;

namespace Thrivts.Application.Common.Security;

/// <summary>
/// Every endpoint requires exactly one of these. Roles are mapped to permissions here, in one
/// place, so "who can do what" is reviewable at a glance and never scattered across controllers.
/// The owner-only permissions (prices, finance, client values) are never granted to a staff role —
/// that is the price-confidentiality guarantee, enforced by the API rather than hidden in the UI.
/// </summary>
public static class Permissions
{
    public const string CatalogRead = "catalog:read";
    public const string SuppliersCreate = "suppliers:create";

    public const string GateWrite = "gate:write";
    public const string GateRead = "gate:read";

    public const string LotsRead = "lots:read";
    public const string LotsCount = "lots:count";
    public const string LotsCosign = "lots:cosign";

    public const string InventoryRead = "inventory:read";
    public const string InventoryManage = "inventory:manage";

    public const string RequirementsRead = "requirements:read";
    public const string RequirementsManage = "requirements:manage";

    public const string OperationsManage = "operations:manage";
    public const string AttendanceManage = "attendance:manage";
    public const string ZonesManage = "zones:manage";
    public const string AnalyticsRead = "analytics:read";

    public const string ItemRequestsCreate = "item-requests:create";
    public const string EscalationsCreate = "escalations:create";

    public const string OwnerAccess = "owner:access";
    public const string UsersManage = "users:manage";

    private static readonly string[] OwnerAppPermissions =
    [
        CatalogRead, SuppliersCreate, GateRead, LotsRead, InventoryRead, RequirementsRead, RequirementsManage, AnalyticsRead, OwnerAccess,
    ];

    private static readonly IReadOnlyDictionary<UserRole, HashSet<string>> ByRole = new Dictionary<UserRole, HashSet<string>>
    {
        [UserRole.Owner] = [.. OwnerAppPermissions, UsersManage],
        [UserRole.Finance] = [.. OwnerAppPermissions],
        [UserRole.Sales] = [.. OwnerAppPermissions],
        [UserRole.GeneralManager] =
        [
            CatalogRead, SuppliersCreate, GateRead, LotsRead, LotsCosign, InventoryRead, RequirementsRead,
            RequirementsManage, OperationsManage, AnalyticsRead, ItemRequestsCreate, EscalationsCreate,
        ],
        [UserRole.Supervisor] =
        [
            CatalogRead, LotsRead, LotsCount, AttendanceManage, EscalationsCreate,
        ],
        [UserRole.InventoryManager] =
        [
            CatalogRead, LotsRead, LotsCosign, InventoryRead, InventoryManage, RequirementsRead, ZonesManage,
            EscalationsCreate, ItemRequestsCreate,
        ],
        [UserRole.GateGuard] =
        [
            CatalogRead, SuppliersCreate, GateWrite, GateRead,
        ],
    };

    public static IEnumerable<string> All => ByRole.Values.SelectMany(p => p).Distinct();

    public static bool Has(UserRole role, string permission) => ByRole.TryGetValue(role, out var set) && set.Contains(permission);

    public static IReadOnlyCollection<string> For(UserRole role) => ByRole.TryGetValue(role, out var set) ? set : [];
}
