using FluentAssertions;

namespace ClinicBook.Api.IntegrationTests;

public sealed class SmokeTests
{
    [Fact]
    public void Placeholder_ShouldPass()
    {
        true.Should().BeTrue();
    }
}
