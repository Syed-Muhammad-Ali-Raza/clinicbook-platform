namespace ClinicBook.Domain.Common;

/// <summary>
/// Base class for value objects.
/// </summary>
public abstract class ValueObject
{
    /// <summary>Gets the atomic values that define equality for the value object.</summary>
    /// <returns>Sequence of equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate(0, HashCode.Combine);
    }
}
