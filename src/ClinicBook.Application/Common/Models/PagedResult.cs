namespace ClinicBook.Application.Common.Models;

/// <summary>
/// Represents a page of results.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Initializes a paged result.</summary>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>Items on the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Total number of items across all pages.</summary>
    public int TotalCount { get; }

    /// <summary>Current 1-based page number.</summary>
    public int Page { get; }

    /// <summary>Page size.</summary>
    public int PageSize { get; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Indicates whether there is a next page.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Indicates whether there is a previous page.</summary>
    public bool HasPreviousPage => Page > 1;
}
