namespace MG.Sonarr.Next.Collections;

/// <summary>
/// Represents a read-only set containing zero or one string value, providing efficient set operations for
/// single-item scenarios.
/// </summary>
/// <remarks>This struct is optimized for cases where a set contains at most one string. All set
/// operations are performed using ordinal, case-insensitive string comparison.</remarks>
internal readonly struct OneStringSet : IReadOnlySet<string>
{
	const int COUNT = 1;

	readonly string? _value;

	/// <summary>
	/// Gets a value that indicates whether this set is empty.
	/// </summary>
	/// <value><see langword="true"/> when this set has no value; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(false, nameof(Value), nameof(_value))]
	public bool IsEmpty => string.IsNullOrEmpty(_value);

	/// <summary>
	/// Gets the single value in this set.
	/// </summary>
	/// <value>The set value, or an empty string when this set is empty.</value>
	public string Value => _value ?? string.Empty;

	int IReadOnlyCollection<string>.Count => this.IsEmpty ? 0 : COUNT;

	/// <summary>
	/// Initializes a new <see cref="OneStringSet"/> instance with the specified value.
	/// </summary>
	/// <param name="value">The set value.</param>
	internal OneStringSet(string value)
	{
		_value = value;
	}

	/// <summary>
	/// Returns an enumerator that iterates through the set.
	/// </summary>
	/// <returns>An enumerator for the set contents.</returns>
	public IEnumerator<string> GetEnumerator()
	{
		if (!this.IsEmpty)
		{
			yield return _value;
		}
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	/// <summary>
	/// Creates a <see cref="OneStringSet"/> from the first value in the specified collection.
	/// </summary>
	/// <param name="collection">The source collection.</param>
	/// <returns>A set containing the first value in <paramref name="collection"/>, or an empty set when the collection has no values.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="collection"/> is null.</exception>
	internal static OneStringSet FromEnumerable(IEnumerable<string> collection)
	{
		ArgumentNullException.ThrowIfNull(collection);

		foreach (string str in collection)
		{
			return new OneStringSet(str);
		}

		return default;
	}

	/// <summary>
	/// Determines whether this set contains the specified value.
	/// </summary>
	/// <param name="item">The value to locate.</param>
	/// <returns><see langword="true"/> when the value is present; otherwise, <see langword="false"/>.</returns>
	bool IReadOnlySet<string>.Contains(string item)
	{
		return !this.IsEmpty && _value.Equals(item, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Determines whether this set is a proper subset of the specified collection.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns><see langword="true"/> when this set is a proper subset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	bool IReadOnlySet<string>.IsProperSubsetOf(IEnumerable<string> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		if (this.IsEmpty)
		{
			return other.Any();
		}

		bool isSubset = false;
		bool hasExtra = false;

		foreach (string s in other)
		{
			if (_value.Equals(s, StringComparison.OrdinalIgnoreCase))
			{
				isSubset = true;
			}
			else
			{
				hasExtra = true;
			}

			if (isSubset && hasExtra)
			{
				return true;
			}
		}

		return false;
	}
	/// <summary>
	/// Determines whether this set is a proper superset of the specified collection.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns><see langword="true"/> when this set is a proper superset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	bool IReadOnlySet<string>.IsProperSupersetOf(IEnumerable<string> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		if (this.IsEmpty)
		{
			return false;
		}

		return !other.Any();
	}
	/// <summary>
	/// Determines whether this set is a subset of the specified collection.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns><see langword="true"/> when this set is a subset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	bool IReadOnlySet<string>.IsSubsetOf(IEnumerable<string> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		if (this.IsEmpty)
		{
			return true;
		}

		foreach (string s in other)
		{
			if (_value.Equals(s, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
	/// <summary>
	/// Determines whether this set is a superset of the specified collection.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns><see langword="true"/> when this set is a superset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	bool IReadOnlySet<string>.IsSupersetOf(IEnumerable<string> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		if (this.IsEmpty)
		{
			return !other.Any();
		}

		foreach (string s in other)
		{
			if (!_value.Equals(s, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}

		return true;
	}
	/// <summary>
	/// Determines whether this set overlaps with the specified collection.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns><see langword="true"/> when this set and <paramref name="other"/> share at least one value; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	bool IReadOnlySet<string>.Overlaps(IEnumerable<string> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		if (this.IsEmpty)
		{
			return false;
		}

		foreach (string s in other)
		{
			if (_value.Equals(s, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
	/// <summary>
	/// Determines whether this set and the specified collection contain the same values.
	/// </summary>
	/// <param name="other">The collection to compare against.</param>
	/// <returns><see langword="true"/> when this set and <paramref name="other"/> are equal; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	bool IReadOnlySet<string>.SetEquals(IEnumerable<string> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		if (this.IsEmpty)
		{
			return !other.Any();
		}

		bool isSubset = false;

		foreach (string s in other)
		{
			if (!_value.Equals(s, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			isSubset = true;
		}

		return isSubset;
	}
}

