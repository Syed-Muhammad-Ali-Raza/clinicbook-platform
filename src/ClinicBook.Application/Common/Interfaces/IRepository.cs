using ClinicBook.Domain.Common;

namespace ClinicBook.Application.Common.Interfaces;

/// <summary>
/// Generic repository abstraction for aggregate roots and entities.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>Gets an entity by identifier.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists entities matching a specification.</summary>
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>Lists all entities.</summary>
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Counts entities matching a specification.</summary>
    Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>Adds a new entity.</summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Updates an entity.</summary>
    void Update(T entity);

    /// <summary>Removes an entity.</summary>
    void Remove(T entity);
}
