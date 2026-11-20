namespace ClinicBook.Application.Common.Interfaces;

/// <summary>
/// Provides information about the current authenticated user.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Current user identifier, if authenticated.</summary>
    Guid? UserId { get; }

    /// <summary>Current user's primary role, if available.</summary>
    string? Role { get; }

    /// <summary>All roles assigned to the current user.</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>Current user's clinic scope, if available.</summary>
    Guid? ClinicId { get; }

    /// <summary>Indicates whether a user is authenticated.</summary>
    bool IsAuthenticated { get; }
}
