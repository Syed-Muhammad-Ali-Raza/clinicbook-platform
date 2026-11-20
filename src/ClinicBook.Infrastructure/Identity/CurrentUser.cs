using ClinicBook.Application.Common.Interfaces;

namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// Default current user implementation. Replaced with HTTP-context-backed implementation in later phases.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    /// <inheritdoc />
    public string? UserId => null;

    /// <inheritdoc />
    public string? Role => null;

    /// <inheritdoc />
    public Guid? ClinicId => null;

    /// <inheritdoc />
    public bool IsAuthenticated => false;
}
