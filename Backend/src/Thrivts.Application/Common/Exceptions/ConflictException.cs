namespace Thrivts.Application.Common.Exceptions;

/// <summary>The request was valid but collides with the current state — someone else changed the
/// record first, or there is not enough stock left. Returned as 409 so the app can reload and retry.</summary>
public class ConflictException(string message) : Exception(message);
