using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Messages;

public sealed record MarkThreadReadCommand(Guid ThreadId) : ICommand<ErrorOr<Success>>;

public sealed class MarkThreadReadCommandHandler : ICommandHandler<MarkThreadReadCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MarkThreadReadCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(MarkThreadReadCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can mark a thread read.");

        var thread = await _db.MessageThreads.FirstOrDefaultAsync(t => t.Id == command.ThreadId, cancellationToken);
        if (thread is null)
            return Error.NotFound(description: $"Thread '{command.ThreadId}' was not found.");

        thread.MarkReadByAdmin();
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
