using ClinicBook.Application.Common.Models;
using FluentAssertions;
using Xunit;

namespace ClinicBook.Application.Tests;

public sealed class PagedQueryTests
{
    private sealed record TestPagedQuery : PagedQuery;

    [Fact]
    public void PageSize_ShouldBeCappedAtMax()
    {
        var query = new TestPagedQuery { PageSize = 500 };

        query.PageSize.Should().Be(PagedQuery.MaxPageSize);
    }

    [Fact]
    public void Skip_ShouldBeCalculatedFromPageAndSize()
    {
        var query = new TestPagedQuery { Page = 3, PageSize = 20 };

        query.Skip.Should().Be(40);
    }

    [Fact]
    public void PageSize_ShouldDefaultWhenInvalid()
    {
        var query = new TestPagedQuery { PageSize = 0 };

        query.PageSize.Should().Be(20);
    }
}
