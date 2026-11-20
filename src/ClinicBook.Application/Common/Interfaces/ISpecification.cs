using System.Linq.Expressions;

namespace ClinicBook.Application.Common.Interfaces;

/// <summary>
/// Encapsulates query logic for reusable data access.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface ISpecification<T>
{
    /// <summary>Filter criteria.</summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>Related entities to include.</summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>String-based includes for nested navigation.</summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>Ascending order expression.</summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>Descending order expression.</summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>Number of items to take.</summary>
    int? Take { get; }

    /// <summary>Number of items to skip.</summary>
    int? Skip { get; }

    /// <summary>Indicates whether paging is enabled.</summary>
    bool IsPagingEnabled { get; }

    /// <summary>Indicates whether tracking is disabled.</summary>
    bool AsNoTracking { get; }
}

/// <summary>
/// Base class for specifications.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];

    /// <inheritdoc />
    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    /// <inheritdoc />
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;

    /// <inheritdoc />
    public IReadOnlyList<string> IncludeStrings => _includeStrings;

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <inheritdoc />
    public int? Take { get; private set; }

    /// <inheritdoc />
    public int? Skip { get; private set; }

    /// <inheritdoc />
    public bool IsPagingEnabled { get; private set; }

    /// <inheritdoc />
    public bool AsNoTracking { get; private set; } = true;

    /// <summary>Adds an include expression.</summary>
    protected void AddInclude(Expression<Func<T, object>> includeExpression) => _includes.Add(includeExpression);

    /// <summary>Adds a string-based include.</summary>
    protected void AddInclude(string includeString) => _includeStrings.Add(includeString);

    /// <summary>Applies paging.</summary>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>Sets ascending ordering.</summary>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) => OrderBy = orderByExpression;

    /// <summary>Sets descending ordering.</summary>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression) => OrderByDescending = orderByDescExpression;

    /// <summary>Enables change tracking for the query.</summary>
    protected void EnableTracking() => AsNoTracking = false;
}
