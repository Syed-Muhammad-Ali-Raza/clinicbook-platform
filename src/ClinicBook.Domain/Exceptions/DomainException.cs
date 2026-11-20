namespace ClinicBook.Domain.Exceptions;

/// <summary>
/// Base exception for domain rule violations.
/// </summary>
public class DomainException : Exception
{
    /// <summary>Initializes a new domain exception.</summary>
    /// <param name="message">Error message.</param>
    public DomainException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new domain exception.</summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
