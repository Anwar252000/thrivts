using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Users;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1.Admin;

/// <summary>Role-agnostic user management — a flat view across Buyer/Seller/Agency/Admin, for
/// finding and managing any account regardless of role. Role-specific detail (company info, tier,
/// commission rate, KYC, etc.) still lives on the dedicated Buyers/Sellers/Agencies pages.</summary>
[Route("api/v{version:apiVersion}/admin/users")]
public class AdminUsersController : AdminControllerBase
{
    public AdminUsersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] UserRole? role, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetUsersQuery(role, search, page, pageSize), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new CreateUserCommand(
                request.Email, request.Password, request.FullName, request.Role, request.Phone, request.WhatsApp,
                request.CompanyName, request.Country, request.PublicAlias, request.LocationCity, request.LocationCountry,
                request.AgencyName, request.CommissionRate),
            cancellationToken);
        return ToResponse(result, id => CreatedAtAction(nameof(Get), new { }, new { id }));
    }

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> Update(Guid userId, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateUserCommand(userId, request.FullName, request.Phone, request.WhatsApp), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteUserCommand(userId), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record CreateUserRequest(
    string Email, string Password, string FullName, UserRole Role, string? Phone, string? WhatsApp,
    string? CompanyName, string? Country, string? PublicAlias, string? LocationCity, string? LocationCountry,
    string? AgencyName, decimal? CommissionRate);

public record UpdateUserRequest(string FullName, string? Phone, string? WhatsApp);
