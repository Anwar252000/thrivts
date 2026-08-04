namespace Thrivts.Application.Common.Exceptions;

/// <summary>
/// Thrown when an authenticated caller is not allowed to access/act on a specific resource
/// (e.g. a buyer id that doesn't belong to them). Distinct from the 401 the auth middleware
/// returns for "not authenticated at all".
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("You do not have permission to perform this action.")
    {
    }
}
