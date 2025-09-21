namespace MG.Sonarr.Next.Strings;

public readonly partial struct Wildcard
{
	#region EQUALITY
	/// <summary>
	/// Determines whether the current <see cref="Wildcard"/> instance is equal to the specified span of characters,
	/// using a case-insensitive comparison.
	/// </summary>
	/// <param name="other">The span of characters to compare with the current instance.</param>
	/// <returns>
	/// <see langword="true"/> if the specified span of characters is equal to the current instance; otherwise, <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	public bool Equals(ReadOnlySpan<char> other)
	{
		return this.Equals(other, DEFAULT_COMPARISON);
	}
	/// <summary>
	/// Determines whether the current <see cref="Wildcard"/> instance is equal to the specified span of characters,
	/// using the specified string comparison option.
	/// </summary>
	/// <param name="other">The span of characters to compare with the current instance.</param>
	/// <param name="comparisonType">The string comparison option to use.</param>
	/// <returns>
	/// <see langword="true"/> if the specified span of characters is equal to the current instance; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Equals(ReadOnlySpan<char> other, StringComparison comparisonType)
	{
		return other.Equals(_pattern, comparisonType);
	}
	/// <summary>
	/// Determines whether the current <see cref="Wildcard"/> instance is equal to the specified string,
	/// using a case-insensitive comparison.
	/// </summary>
	/// <param name="other">The string to compare with the current instance.</param>
	/// <returns>
	/// <see langword="true"/> if the specified string is equal to the current instance, ignoring case; otherwise, <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	public bool Equals(string? other)
	{
		return this.Equals(other, DEFAULT_COMPARISON);
	}
	/// <summary>
	/// Determines whether the current <see cref="Wildcard"/> instance is equal to the specified string,
	/// using the specified string comparison option.
	/// </summary>
	/// <param name="other">The string to compare with the current instance.</param>
	/// <param name="comparisonType">The string comparison option to use.</param>
	/// <returns>
	/// <see langword="true"/> if the specified string is equal to the current instance; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Equals(string? other, StringComparison comparisonType)
	{
		return ReferenceEquals(_pattern, other) || this.Equals(other.AsSpan(), comparisonType);
	}
	/// <summary>
	/// Determines whether the current <see cref="Wildcard"/> instance is equal to another <see cref="Wildcard"/> instance.
	/// </summary>
	/// <param name="other">The other <see cref="Wildcard"/> instance to compare with.</param>
	/// <param name="comparisonType">
	/// The string comparison rules to use when comparing the two <see cref="Wildcard"/> instances.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the two <see cref="Wildcard"/> instances are equal; otherwise, <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	public readonly bool Equals(Wildcard other)
	{
		return this.Equals(other, DEFAULT_COMPARISON);
	}
	/// <summary>
	/// Determines whether the current <see cref="Wildcard"/> instance is equal to another <see cref="Wildcard"/> instance.
	/// </summary>
	/// <param name="other">The other <see cref="Wildcard"/> instance to compare with.</param>
	/// <param name="comparisonType">
	/// The string comparison rules to use when comparing the two <see cref="Wildcard"/> instances.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the two <see cref="Wildcard"/> instances are equal; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Equals(Wildcard other, StringComparison comparisonType)
	{
		if (this.IsEmpty)
		{
			return other.IsEmpty;
		}

		return _state.Equals(other._state)
			&& _pattern.AsSpan().Equals(other._pattern, comparisonType);
	}
	/// <summary>
	/// Determines whether the current <see cref="Wildcard"/> instance is equal to the specified object.
	/// </summary>
	/// <param name="obj">The object to compare with the current instance.</param>
	/// <returns>
	/// <see langword="true"/> if the specified object is equal to the current instance; otherwise, <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Wildcard other)
		{
			return this.Equals(other, DEFAULT_COMPARISON);
		}
		else if (obj is string s)
		{
			return this.Equals(s, DEFAULT_COMPARISON);
		}

		return false;
	}
	/// <summary>
	/// Returns the hash code for the current <see cref="Wildcard"/> instance.
	/// </summary>
	/// <returns>The hash code for the current <see cref="Wildcard"/> instance.</returns>
	public override readonly int GetHashCode()
	{
		if (this.IsEmpty)
		{
			return HashCode.Combine(value1: true, string.Empty, WildcardMatchType.None, 0);
		}

		return HashCode.Combine(value1: false, StringComparer.OrdinalIgnoreCase.GetHashCode(_pattern), _state.Type, _state.Length);
	}

	#endregion

	#region OPERATORS

	/// <summary>
	/// Implicitly converts a <see cref="string"/> to a <see cref="Wildcard"/> instance.
	/// </summary>
	/// <param name="pattern">The string to convert to a <see cref="Wildcard"/> instance.</param>
	/// <returns>A <see cref="Wildcard"/> instance that represents the specified string.</returns>
	[DebuggerStepThrough]
	public static implicit operator Wildcard(string? pattern)
	{
		return Parse(pattern);
	}
	/// <summary>
	/// Explicitly converts a <see cref="Wildcard"/> instance to a <see cref="string"/>.
	/// </summary>
	/// <param name="pattern">The <see cref="Wildcard"/> instance to convert to a <see cref="string"/>.</param>
	/// <returns>A <see cref="string"/> that represents the specified <see cref="Wildcard"/> instance.</returns>
	[DebuggerStepThrough]
	public static explicit operator string(Wildcard pattern)
	{
		return pattern.ToString();
	}
	/// <summary>
	/// Implicitly converts a <see cref="Wildcard"/> instance to a <see cref="ReadOnlySpan{char}"/>.
	/// </summary>
	/// <param name="pattern">The <see cref="Wildcard"/> instance to convert.</param>
	/// <returns>A <see cref="ReadOnlySpan{char}"/> that represents the specified <see cref="Wildcard"/> instance.</returns>
	[DebuggerStepThrough]
	public static implicit operator ReadOnlySpan<char>(Wildcard pattern)
	{
		return pattern.AsSpan();
	}

	/// <summary>
	/// Determines whether two <see cref="Wildcard"/> instances are equal.
	/// </summary>
	/// <param name="x">The first <see cref="Wildcard"/> instance to compare.</param>
	/// <param name="y">The second <see cref="Wildcard"/> instance to compare.</param>
	/// <returns><see langword="true"/> if the two <see cref="Wildcard"/> instances are equal; otherwise, <see langword="false"/>.</returns>
	[DebuggerStepThrough]
	public static bool operator ==(Wildcard x, Wildcard y)
	{
		return x.Equals(y);
	}
	/// <summary>
	/// Determines whether two <see cref="Wildcard"/> instances are not equal.
	/// </summary>
	/// <param name="x">The first <see cref="Wildcard"/> instance to compare.</param>
	/// <param name="y">The second <see cref="Wildcard"/> instance to compare.</param>
	/// <returns><see langword="true"/> if the two <see cref="Wildcard"/> instances are not equal; otherwise, <see langword="false"/>.</returns>
	[DebuggerStepThrough]
	public static bool operator !=(Wildcard x, Wildcard y)
	{
		return !(x == y);
	}

	public static bool operator ==(Wildcard x, string? y)
	{
		return x.Equals(y);
	}
	public static bool operator !=(Wildcard x, string? y)
	{
		return !(x == y);
	}
	public static bool operator ==(string? x, Wildcard y)
	{
		return y.Equals(x);
	}
	public static bool operator !=(string? x, Wildcard y)
	{
		return !(x == y);
	}

	#endregion
}