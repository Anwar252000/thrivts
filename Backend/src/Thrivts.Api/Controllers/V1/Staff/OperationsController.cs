using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Api.Common;
using Thrivts.Api.Filters;
using Thrivts.Application.Common.Security;
using Thrivts.Application.Features.Dashboards;
using Thrivts.Application.Features.Operations;
using Thrivts.Application.Features.Owner;
using Thrivts.Domain.Operations;

namespace Thrivts.Api.Controllers.V1.Staff;

/// <summary>The general manager's daily sheet: expenses, blockers, team check, day summary, and the
/// "submitted today" ticks that feed the owner's compliance view.</summary>
[Route("api/v{version:apiVersion}/operations")]
public sealed class OperationsController(IMediator mediator, IAuthorizationService authorization) : ApiControllerBase(mediator)
{
    [HttpGet("day")]
    [Authorize(Policy = Permissions.OperationsManage)]
    public async Task<DaySheetDto> Day([FromQuery] DateOnly? date, CancellationToken ct) =>
        await Mediator.Send(new GetDaySheetQuery(date), ct);

    [HttpGet("overview")]
    [Authorize(Policy = Permissions.AnalyticsRead)]
    public async Task<OperationsOverviewDto> Overview(CancellationToken ct) =>
        await Mediator.Send(new GetOperationsOverviewQuery(), ct);

    [HttpPost("expenses")]
    [Idempotent]
    [Authorize(Policy = Permissions.OperationsManage)]
    public async Task<IActionResult> AddExpense([FromBody] SaveExpenseCommand command, CancellationToken ct) =>
        ToCreated(await Mediator.Send(command with { Id = null }, ct));

    [HttpPut("expenses/{id:guid}")]
    [Authorize(Policy = Permissions.OperationsManage)]
    public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] SaveExpenseCommand command, CancellationToken ct) =>
        ToResponse(await Mediator.Send(command with { Id = id }, ct));

    [HttpDelete("expenses/{id:guid}")]
    [Authorize(Policy = Permissions.OperationsManage)]
    public async Task<IActionResult> DeleteExpense(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DeleteExpenseCommand(id), ct));

    [HttpPost("blockers")]
    [Idempotent]
    [Authorize(Policy = Permissions.OperationsManage)]
    public async Task<IActionResult> AddBlocker([FromBody] SaveBlockerCommand command, CancellationToken ct) =>
        ToCreated(await Mediator.Send(command with { Id = null }, ct));

    [HttpPut("blockers/{id:guid}")]
    [Authorize(Policy = Permissions.OperationsManage)]
    public async Task<IActionResult> UpdateBlocker(Guid id, [FromBody] SaveBlockerCommand command, CancellationToken ct) =>
        ToResponse(await Mediator.Send(command with { Id = id }, ct));

    [HttpDelete("blockers/{id:guid}")]
    [Authorize(Policy = Permissions.OperationsManage)]
    public async Task<IActionResult> DeleteBlocker(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DeleteBlockerCommand(id), ct));

    [HttpPut("summary")]
    [Authorize(Policy = Permissions.OperationsManage)]
    public async Task<IActionResult> SaveSummary([FromBody] SaveDaySummaryCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPut("team-check")]
    [Authorize(Policy = Permissions.OperationsManage)]
    public async Task<IActionResult> SaveTeamCheck([FromBody] SaveTeamCheckCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>Marks a daily section as submitted. Each section belongs to a different role, so the
    /// permission is decided by the section in the body rather than by the route.</summary>
    [HttpPost("submissions")]
    public async Task<IActionResult> Submit([FromBody] SubmitSectionCommand command, CancellationToken ct)
    {
        var permission = command.Section switch
        {
            DailySection.Requirements => Permissions.RequirementsManage,
            DailySection.Warehouse => Permissions.ZonesManage,
            _ => Permissions.OperationsManage,
        };

        if (!(await authorization.AuthorizeAsync(User, permission)).Succeeded)
            return Forbid();

        await Mediator.Send(command, ct);
        return NoContent();
    }

    [HttpGet("compliance")]
    [Authorize(Policy = Permissions.AnalyticsRead)]
    public async Task<ComplianceDto> Compliance([FromQuery] int days = 7, CancellationToken ct = default) =>
        await Mediator.Send(new GetComplianceQuery(days), ct);

    [HttpGet("zones")]
    [Authorize(Policy = Permissions.ZonesManage)]
    public async Task<IReadOnlyList<ZoneCheckDto>> Zones([FromQuery] DateOnly? date, CancellationToken ct) =>
        await Mediator.Send(new GetZoneChecksQuery(date), ct);

    [HttpPut("zones")]
    [Authorize(Policy = Permissions.ZonesManage)]
    public async Task<IActionResult> SaveZones([FromBody] SaveZoneChecksCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>The GM asks the owner to approve a spend or decision; the owner decides in the owner app.</summary>
    [HttpPost("approvals")]
    [Idempotent]
    [Authorize(Policy = Permissions.OperationsManage)]
    public async Task<IActionResult> SubmitApproval([FromBody] SubmitApprovalCommand command, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, new IdResponse(await Mediator.Send(command, ct)));
}

[Route("api/v{version:apiVersion}/attendance")]
[Authorize(Policy = Permissions.AttendanceManage)]
public sealed class AttendanceController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet("day")]
    public async Task<AttendanceDayDto> Day([FromQuery] DateOnly? date, CancellationToken ct) =>
        await Mediator.Send(new GetAttendanceDayQuery(date), ct);

    [HttpPut("day/{date}/labourers/{labourerId:guid}")]
    public async Task<IActionResult> Set(DateOnly date, Guid labourerId, [FromBody] SetAttendanceRequest request, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new SetAttendanceCommand(date, labourerId, request.Present, request.TimeIn, request.TimeOut), ct));

    [HttpPost("day/{date}/everyone-present")]
    public async Task<IActionResult> EveryonePresent(DateOnly date, CancellationToken ct)
    {
        await Mediator.Send(new MarkEveryonePresentCommand(date), ct);
        return NoContent();
    }

    /// <summary>Which recent workdays have attendance filled in — the supervisor's dashboard ring.</summary>
    [HttpGet("history")]
    public async Task<AttendanceLogHistoryDto> History([FromQuery] int days = 30, CancellationToken ct = default) =>
        await Mediator.Send(new GetAttendanceLogHistoryQuery(days), ct);

    [HttpGet("overtime")]
    public async Task<IReadOnlyList<MonthlyOvertimeRow>> Overtime([FromQuery] int year, [FromQuery] int month, CancellationToken ct) =>
        await Mediator.Send(new GetMonthlyOvertimeQuery(year, month), ct);

    [HttpGet("export.csv")]
    public Task Export([FromQuery] int year, [FromQuery] int month, CancellationToken ct) =>
        CsvResults.WriteAsync(Response, $"attendance-{year:D4}-{month:D2}.csv",
            ["Date", "Day", "Worker", "Time in", "Time out", "Overtime hours", "Sunday"],
            Mediator.CreateStream(new ExportAttendanceQuery(year, month), ct),
            r => [r.Date, r.Day, r.Worker, r.TimeIn, r.TimeOut, r.OvertimeHours, r.Sunday],
            ct);
}

public sealed record SetAttendanceRequest(bool Present, TimeOnly? TimeIn, TimeOnly? TimeOut);
