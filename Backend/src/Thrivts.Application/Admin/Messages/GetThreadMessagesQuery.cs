using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Messages;

public sealed record GetThreadMessagesQuery(Guid ThreadId) : IQuery<ErrorOr<List<MessageDto>>>;

public sealed record MessageDto(Guid Id, Guid SenderId, MessageSenderType SenderType, string Body, bool IsRead, DateTimeOffset CreatedAt);

public sealed class GetThreadMessagesQueryHandler : IQueryHandler<GetThreadMessagesQuery, ErrorOr<List<MessageDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetThreadMessagesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<MessageDto>>> Handle(GetThreadMessagesQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view thread messages.");

        var result = await _db.Messages.AsNoTracking()
            .Where(m => m.ThreadId == query.ThreadId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(m.Id, m.SenderId, m.SenderType, m.Body, m.IsRead, m.CreatedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
