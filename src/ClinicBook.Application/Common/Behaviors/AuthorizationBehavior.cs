using ClinicBook.Application.Common.Exceptions;
using ClinicBook.Application.Common.Interfaces;
using MediatR;

namespace ClinicBook.Application.Common.Behaviors;

/// <summary>
/// Enforces role-based authorization for requests implementing <see cref="IAuthorizedRequest"/>.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes the behavior.</summary>
    public AuthorizationBehavior(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IAuthorizedRequest authorizedRequest)
        {
            return next();
        }

        if (!_currentUser.IsAuthenticated)
        {
            throw new ForbiddenAccessException("Authentication is required.");
        }

        if (authorizedRequest.Roles.Count > 0)
        {
            var userRole = _currentUser.Role;
            if (string.IsNullOrWhiteSpace(userRole) || !authorizedRequest.Roles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
            {
                throw new ForbiddenAccessException("User does not have a required role.");
            }
        }

        // Policy-based and resource-based checks are implemented in later phases.
        return next();
    }
}
