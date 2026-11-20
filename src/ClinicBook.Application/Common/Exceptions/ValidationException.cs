using FluentValidation.Results;

namespace ClinicBook.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when request validation fails.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>Initializes a new validation exception.</summary>
    public ValidationException() : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>Initializes a new validation exception from FluentValidation failures.</summary>
    /// <param name="failures">Validation failures.</param>
    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    /// <summary>Validation errors grouped by property name.</summary>
    public IDictionary<string, string[]> Errors { get; }
}
