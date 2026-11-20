using ClinicBook.Application.Common.Interfaces;

namespace ClinicBook.Infrastructure.Time;

/// <summary>
/// System clock implementation.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
