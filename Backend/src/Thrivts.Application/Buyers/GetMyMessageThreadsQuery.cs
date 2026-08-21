using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Buyers;

/// <summary>Replaces buyer.html's loadMessages() — buyer.html itself calls this a "basic version",
/// listing threads only (no message-body preview; Message is a separate table not yet joined here).</summary>
public sealed record GetMyMessageThreadsQuery : IQuery<ErrorOr<List<MyMessageThreadDto>>>;

public sealed record MyMessageThreadDto(Guid Id, string? Subject, bool IsOpen, bool Unread, DateTimeOffset? LastMessageAt);

public sealed class GetMyMessageThreadsQueryHandler : IQueryHandler<GetMyMessageThreadsQuery, ErrorOr<List<MyMessageThreadDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyMessageThreadsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<MyMessageThreadDto>>> Handle(GetMyMessageThreadsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var result = await _db.MessageThreads.AsNoTracking()
            .Where(t => t.ParticipantRole == UserRole.Buyer && t.ParticipantId == _currentUser.UserId)
            .OrderByDescending(t => t.LastMessageAt)
            .Take(300)
            .Select(t => new MyMessageThreadDto(t.Id, t.Subject, t.IsOpen, t.UnreadForParticipant, t.LastMessageAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
