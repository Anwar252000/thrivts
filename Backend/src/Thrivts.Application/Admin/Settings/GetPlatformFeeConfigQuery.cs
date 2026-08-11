using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Settings;

public sealed record GetPlatformFeeConfigQuery : IQuery<ErrorOr<PlatformFeeConfigDto>>;

public sealed record PlatformFeeConfigDto(decimal FeePerPcUsd, string PkrReference, DateTimeOffset UpdatedAt);

public sealed class GetPlatformFeeConfigQueryHandler : IQueryHandler<GetPlatformFeeConfigQuery, ErrorOr<PlatformFeeConfigDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPlatformFeeConfigQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<PlatformFeeConfigDto>> Handle(GetPlatformFeeConfigQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view the platform fee config.");

        var config = await _db.PlatformFeeConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.Id, cancellationToken);
        if (config is null)
            return Error.NotFound(description: "Platform fee config has not been set up yet.");

        return new PlatformFeeConfigDto(config.FeePerPcUsd, config.PkrReference, config.UpdatedAt);
    }
}
