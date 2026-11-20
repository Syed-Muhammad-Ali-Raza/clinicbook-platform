namespace ClinicBook.Application.Common.Interfaces;

/// <summary>
/// Abstraction over system time for testability.
/// </summary>
public interface IClock
{
    /// <summary>Gets the current UTC time.</summary>
    DateTime UtcNow { get; }
}
