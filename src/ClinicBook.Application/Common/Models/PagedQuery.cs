namespace ClinicBook.Application.Common.Models;

/// <summary>
/// Base query for paginated requests.
/// </summary>
public abstract record PagedQuery
{
    /// <summary>Maximum allowed page size.</summary>
    public const int MaxPageSize = 100;

    private int _pageSize = 20;

    /// <summary>1-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Number of items per page (capped at <see cref="MaxPageSize"/>).</summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? 20 : value;
    }

    /// <summary>Optional sort expression.</summary>
    public string? Sort { get; init; }

    /// <summary>Optional search term.</summary>
    public string? Search { get; init; }

    /// <summary>Number of items to skip.</summary>
    public int Skip => (Math.Max(Page, 1) - 1) * PageSize;
}
