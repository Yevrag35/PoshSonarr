namespace MG.Sonarr.Next.Components;

/// <summary>
/// Represents a key for a PowerShell type, including optional brackets.
/// </summary>
[DebuggerStepThrough]
[StructLayout(LayoutKind.Auto)]
public readonly struct PSTypeKey : IEquatable<PSTypeKey>
{
    private readonly bool _isNotEmpty;

    /// <summary>
    /// Gets a value indicating whether to include brackets.
    /// </summary>
    public readonly bool IncludeBrackets;

    /// <summary>
    /// Gets a value indicating whether the key is default or empty.
    /// </summary>
    [MemberNotNullWhen(false, nameof(KeyedType))]
    public readonly bool IsDefaultOrEmpty => !_isNotEmpty;

    /// <summary>
    /// Gets the type associated with the key.
    /// </summary>
    public readonly Type? KeyedType;

    /// <summary>
    /// Initializes a new instance of the <see cref="PSTypeKey"/> struct with the specified type.
    /// </summary>
    /// <param name="type">The type to associate with the key.</param>
    private PSTypeKey(Type type)
        : this(type, includeBrackets: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PSTypeKey"/> struct with the specified type and bracket inclusion.
    /// </summary>
    /// <param name="type">The type to associate with the key.</param>
    /// <param name="includeBrackets">A value indicating whether to include brackets.</param>
    public PSTypeKey(Type type, bool includeBrackets)
    {
        ArgumentNullException.ThrowIfNull(type);
        IncludeBrackets = includeBrackets;
        KeyedType = type;
        _isNotEmpty = true;
    }

    /// <summary>
    /// Determines whether the specified <see cref="PSTypeKey"/> is equal to the current <see cref="PSTypeKey"/>.
    /// </summary>
    /// <param name="other">The <see cref="PSTypeKey"/> to compare with the current <see cref="PSTypeKey"/>.</param>
    /// <returns><see langword="true"/> if the specified <see cref="PSTypeKey"/> is equal to the current <see cref="PSTypeKey"/>; otherwise, <see langword="false"/>.</returns>
    public readonly bool Equals(PSTypeKey other)
    {
        if (this.IsDefaultOrEmpty)
        {
            return other.IsDefaultOrEmpty;
        }

        return IncludeBrackets == other.IncludeBrackets && KeyedType.Equals(other.KeyedType);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="PSTypeKey"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="PSTypeKey"/>.</param>
    /// <returns><see langword="true"/> if the specified object is equal to the current <see cref="PSTypeKey"/>; otherwise, <see langword="false"/>.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is PSTypeKey other && this.Equals(other);
    }

    /// <summary>
    /// Serves as a hash function for the <see cref="PSTypeKey"/>.
    /// </summary>
    /// <returns>A hash code for the current <see cref="PSTypeKey"/>.</returns>
    public override readonly int GetHashCode()
    {
        if (this.IsDefaultOrEmpty)
        {
            return HashCode.Combine(true, false, nameof(PSTypeKey));
        }

        return HashCode.Combine(IncludeBrackets, KeyedType);
    }

    /// <summary>
    /// Determines whether two specified instances of <see cref="PSTypeKey"/> are equal.
    /// </summary>
    /// <param name="left">The first <see cref="PSTypeKey"/> to compare.</param>
    /// <param name="right">The second <see cref="PSTypeKey"/> to compare.</param>
    /// <returns><see langword="true"/> if the two <see cref="PSTypeKey"/> instances are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(PSTypeKey left, PSTypeKey right) => left.Equals(right);

    /// <summary>
    /// Determines whether two specified instances of <see cref="PSTypeKey"/> are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="PSTypeKey"/> to compare.</param>
    /// <param name="right">The second <see cref="PSTypeKey"/> to compare.</param>
    /// <returns><see langword="true"/> if the two <see cref="PSTypeKey"/> instances are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(PSTypeKey left, PSTypeKey right) => !(left == right);

    /// <summary>
    /// Implicitly converts a <see cref="Type"/> to a <see cref="PSTypeKey"/>.
    /// </summary>
    /// <param name="type">The type to convert.</param>
    public static implicit operator PSTypeKey(Type type) => new PSTypeKey(type);
}