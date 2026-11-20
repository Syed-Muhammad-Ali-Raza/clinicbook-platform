using FluentAssertions;
using Xunit;

namespace ClinicBook.Infrastructure.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void Placeholder_ShouldPass()
    {
        true.Should().BeTrue();
    }
}
