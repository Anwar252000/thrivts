using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Public;

/// <summary>Replaces get_agency_public_name — a case-insensitive agency-code lookup for the
/// signup form's "Referred by" banner. Exposes only the agency's display name, never anything
/// else about the agency.</summary>
public sealed record GetAgencyPublicNameQuery(string Code) : IQuery<ErrorOr<string?>>;

public sealed class GetAgencyPublicNameQueryHandler : IQueryHandler<GetAgencyPublicNameQuery, ErrorOr<string?>>
{
    private readonly IApplicationDbContext _db;

    public GetAgencyPublicNameQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async ValueTask<ErrorOr<string?>> Handle(GetAgencyPublicNameQuery query, CancellationToken cancellationToken)
    {
        var code = query.Code.ToLower();
        var agency = await _db.Agencies.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AgencyCode.ToLower() == code, cancellationToken);

        return agency?.AgencyName;
    }
}
