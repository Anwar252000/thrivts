using System.Security.Cryptography;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Common;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.Common.Services;

public sealed class ActivityRecorder(IAppDbContext db, IClock clock, ICurrentUser user)
{
    public void Record(ActivityArea area, string type, string text, Guid? referenceId = null) =>
        db.ActivityEvents.Add(new ActivityEvent(area, type, text, user.UserId, user.Name, clock.UtcNow, referenceId));
}

/// <summary>Accepts a drawn signature as a PNG data URL, verifies it really is a PNG of sane size,
/// and stores it once. Rejecting anything else keeps this from being a generic file-upload hole.</summary>
public sealed class SignatureStore(IAppDbContext db, IClock clock, ICurrentUser user)
{
    public const int MaxBytes = 200 * 1024;
    private const string DataUrlPrefix = "data:image/png;base64,";
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public Guid Store(string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith(DataUrlPrefix, StringComparison.Ordinal))
            throw new DomainException("Please sign before confirming.");

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(dataUrl[DataUrlPrefix.Length..]);
        }
        catch (FormatException)
        {
            throw new DomainException("The signature could not be read — please sign again.");
        }

        if (bytes.Length is < 100 or > MaxBytes || !bytes.AsSpan(0, PngMagic.Length).SequenceEqual(PngMagic))
            throw new DomainException("The signature could not be read — please sign again.");

        var file = new StoredFile(FilePurpose.Signature, "image/png", bytes, Convert.ToHexStringLower(SHA256.HashData(bytes)), clock.UtcNow, user.UserId);
        db.StoredFiles.Add(file);
        return file.Id;
    }
}
