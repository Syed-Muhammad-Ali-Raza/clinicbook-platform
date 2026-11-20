using MediatR;

namespace ClinicBook.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands.
/// </summary>
/// <typeparam name="TResponse">Response type.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>
/// Marker interface for commands without a response value.
/// </summary>
public interface ICommand : IRequest;

/// <summary>
/// Marker interface for queries.
/// </summary>
/// <typeparam name="TResponse">Response type.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;

/// <summary>
/// Marker interface for commands that must execute within a transaction.
/// </summary>
public interface ITransactionalCommand;

/// <summary>
/// Marker interface for cacheable queries.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>Cache key for the query.</summary>
    string CacheKey { get; }

    /// <summary>Optional cache duration.</summary>
    TimeSpan? CacheDuration => null;
}

/// <summary>
/// Marker interface for requests that require authorization.
/// </summary>
public interface IAuthorizedRequest
{
    /// <summary>Required roles, if any.</summary>
    IReadOnlyCollection<string> Roles => Array.Empty<string>();

    /// <summary>Required policies, if any.</summary>
    IReadOnlyCollection<string> Policies => Array.Empty<string>();
}
