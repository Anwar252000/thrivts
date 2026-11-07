using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Common;

namespace Thrivts.Application.Features.Files;

public sealed record StoredFileContent(string ContentType, byte[] Content, string Sha256, DateTimeOffset CreatedAt);

/// <summary>Signature images for a lot's audit trail. Content is immutable once written, so the
/// API can serve it with a long cache lifetime and the hash as its ETag.</summary>
public sealed record GetSignatureQuery(Guid Id) : IQuery<ErrorOr<StoredFileContent>>;

public sealed class GetSignatureHandler(IAppDbContext db) : IQueryHandler<GetSignatureQuery, ErrorOr<StoredFileContent>>
{
    public async ValueTask<ErrorOr<StoredFileContent>> Handle(GetSignatureQuery query, CancellationToken ct)
    {
        var file = await db.StoredFiles.AsNoTracking()
            .Where(f => f.Id == query.Id && f.Purpose == FilePurpose.Signature)
            .Select(f => new StoredFileContent(f.ContentType, f.Content, f.Sha256, f.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return file is null ? Error.NotFound("File.NotFound", "Signature not found.") : file;
    }
}
