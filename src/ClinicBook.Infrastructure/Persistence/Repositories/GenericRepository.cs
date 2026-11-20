using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ClinicBook.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository implementation.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    /// <summary>Database context.</summary>
    protected AppDbContext DbContext { get; }

    /// <summary>Entity set.</summary>
    protected DbSet<T> DbSet { get; }

    /// <summary>Initializes a new repository.</summary>
    /// <param name="dbContext">Database context.</param>
    public GenericRepository(AppDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<T>();
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification, applyPaging: false).CountAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return entity;
    }

    /// <inheritdoc />
    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    /// <inheritdoc />
    public virtual void Remove(T entity)
    {
        DbSet.Remove(entity);
    }

    /// <summary>Applies a specification to the entity query.</summary>
    protected IQueryable<T> ApplySpecification(ISpecification<T> specification, bool applyPaging = true)
    {
        IQueryable<T> query = DbSet;

        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (applyPaging && specification.IsPagingEnabled)
        {
            if (specification.Skip.HasValue)
            {
                query = query.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }
        }

        return query;
    }
}
