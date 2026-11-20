namespace ClinicBook.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when the current user is not authorized to perform an action.
/// </summary>
public sealed class ForbiddenAccessException : Exception
{
    /// <summary>Initializes a new forbidden access exception.</summary>
    public ForbiddenAccessException() : base("Access is forbidden.")
    {
    }

    /// <summary>Initializes a new forbidden access exception with a message.</summary>
    /// <param name="message">Error message.</param>
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
