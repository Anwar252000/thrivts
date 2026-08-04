namespace Thrivts.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated (e.g. an invalid deal state transition).
/// Never used for expected/handled business outcomes — those go through ErrorOr in the Application layer.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
