using Mediator;
using Microsoft.AspNetCore.Authorization;
using Thrivts.Api.Controllers.V1;

namespace Thrivts.Api.Controllers.V1.Admin;

/// <summary>Shared base for every Admin/* controller — adds the AdminOnly policy on top of
/// ApiControllerBase's versioning + ErrorOr-to-HTTP-status mapping.</summary>
[Authorize(Policy = "AdminOnly")]
public abstract class AdminControllerBase : ApiControllerBase
{
    protected AdminControllerBase(IMediator mediator) : base(mediator)
    {
    }
}
