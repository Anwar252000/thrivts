namespace Thrivts.Application.Common.Interfaces;

/// <summary>
/// Supabase Storage's REST API (service_role key — same key/separation reasoning as
/// ISupabaseAdminClient). Used for the buyer receipt-upload flow: the buyer sends file bytes to
/// *our* API (already [Authorize(Policy="BuyerOnly")]-gated and ownership-checked), and this client
/// pushes them to Storage server-side — the browser never talks to Supabase Storage directly,
/// preserving the ".NET API is the single client of Supabase" rule.
/// </summary>
public interface ISupabaseStorageClient
{
    /// <summary>Uploads a file to the given bucket/path, overwriting if it already exists. Returns
    /// the stored path (same as the input path — kept as a return value so callers don't have to
    /// track the exact path string separately).</summary>
    Task<string> UploadAsync(string bucket, string path, byte[] content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Creates a time-limited signed URL for a private bucket object (the `receipts`
    /// bucket is not public) — used when admin/buyer need to view an uploaded receipt.</summary>
    Task<string> CreateSignedUrlAsync(string bucket, string path, int expiresInSeconds, CancellationToken cancellationToken = default);
}
