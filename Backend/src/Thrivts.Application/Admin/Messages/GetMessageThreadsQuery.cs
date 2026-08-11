using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Messages;

public sealed record GetMessageThreadsQuery(bool? UnreadOnly) : IQuery<ErrorOr<List<MessageThreadListItemDto>>>;

public sealed record MessageThreadListItemDto(
    Guid Id, UserRole ParticipantRole, Guid ParticipantId, string? Subject, bool IsOpen,
    DateTimeOffset? LastMessageAt, int UnreadCount);

public sealed class GetMessageThreadsQueryHandler : IQueryHandler<GetMessageThreadsQuery, ErrorOr<List<MessageThreadListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMessageThreadsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<MessageThreadListItemDto>>> Handle(GetMessageThreadsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list message threads.");

        var threads = _db.MessageThreads.AsNoTracking();
        if (query.UnreadOnly == true)
            threads = threads.Where(t => t.UnreadForAdmin);

        var result = await threads
            .OrderByDescending(t => t.LastMessageAt)
            .Select(t => new MessageThreadListItemDto(t.Id, t.ParticipantRole, t.ParticipantId, t.Subject, t.IsOpen, t.LastMessageAt, t.UnreadCount))
            .ToListAsync(cancellationToken);

        return result;
    }
}
