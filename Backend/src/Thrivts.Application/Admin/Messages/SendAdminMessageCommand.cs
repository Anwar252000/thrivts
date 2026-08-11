using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Messages;

public sealed record SendAdminMessageCommand(Guid ThreadId, string Body) : ICommand<ErrorOr<Guid>>;

public sealed class SendAdminMessageCommandHandler : ICommandHandler<SendAdminMessageCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public SendAdminMessageCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(SendAdminMessageCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can send this message.");

        var thread = await _db.MessageThreads.FirstOrDefaultAsync(t => t.Id == command.ThreadId, cancellationToken);
        if (thread is null)
            return Error.NotFound(description: $"Thread '{command.ThreadId}' was not found.");

        var message = new Message(command.ThreadId, _currentUser.UserId.Value, MessageSenderType.Admin, command.Body);
        _db.Messages.Add(message);

        thread.RecordIncomingFromAdmin(_clock.UtcNow);

        await _db.SaveChangesAsync(cancellationToken);

        return message.Id;
    }
}
