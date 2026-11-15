namespace MG.Sonarr.Next.Generators;

/// <summary>
/// Represents an array that equals another when their elements are equal, one by one and in order.
/// </summary>
/// <remarks>
/// An array compares by reference, so a model that holds one never equals the model that the next run builds, and the
/// incremental pipeline renders its source again after every edit to the compilation. Wrapping the array gives the
/// model's record equality the element-by-element comparison it needs. The wrapper doesn't copy the array, so the
/// array mustn't change after it's wrapped.
/// </remarks>
/// <typeparam name="T">The element type.</typeparam>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
	where T : IEquatable<T>
{
	private readonly T[]? _array;

	/// <summary>Initializes a new <see cref="EquatableArray{T}"/> instance that wraps the specified array.</summary>
	/// <param name="array">The array to wrap, which mustn't change afterward.</param>
	public EquatableArray(T[] array)
	{
		_array = array;
	}

	/// <summary>Gets the number of elements.</summary>
	/// <value>The array's length, or 0 for a default instance.</value>
	public int Length => _array?.Length ?? 0;

	/// <summary>Gets the element at the specified index.</summary>
	/// <param name="index">The zero-based index of the element.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> is outside the array.</exception>
	/// <value>The element at <paramref name="index"/>.</value>
	public T this[int index] => this.AsSpan()[index];

	/// <summary>Returns a read-only span over the elements.</summary>
	/// <returns>A span over the array, which is empty for a default instance.</returns>
	public ReadOnlySpan<T> AsSpan() => _array;

	/// <summary>Returns an enumerator over the elements, so the array can be read with <see langword="foreach"/>.</summary>
	/// <returns>The span enumerator over the elements.</returns>
	public ReadOnlySpan<T>.Enumerator GetEnumerator() => this.AsSpan().GetEnumerator();

	/// <summary>Determines whether this array holds the same elements as another, in the same order.</summary>
	/// <param name="other">The array to compare with.</param>
	/// <returns><see langword="true"/> if both have the same length and equal elements at each index; otherwise, <see langword="false"/>.</returns>
	public bool Equals(EquatableArray<T> other) => this.AsSpan().SequenceEqual(other.AsSpan());

	/// <summary>Determines whether an object is an <see cref="EquatableArray{T}"/> that holds the same elements, in the same order.</summary>
	/// <param name="obj">The object to compare with.</param>
	/// <returns><see langword="true"/> if <paramref name="obj"/> is an equal array; otherwise, <see langword="false"/>.</returns>
	public override bool Equals(object? obj) => obj is EquatableArray<T> other && this.Equals(other);

	/// <summary>Returns a hash code that combines the elements' hash codes, in order.</summary>
	/// <returns>The hash code.</returns>
	public override int GetHashCode()
	{
		int hash = 17;
		foreach (T item in this.AsSpan())
		{
			hash = unchecked((hash * 31) + (item is null ? 0 : item.GetHashCode()));
		}

		return hash;
	}

	/// <summary>Determines whether two arrays hold the same elements, in the same order.</summary>
	/// <param name="left">The first array.</param>
	/// <param name="right">The second array.</param>
	/// <returns><see langword="true"/> if the arrays are equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

	/// <summary>Determines whether two arrays differ in length or in an element.</summary>
	/// <param name="left">The first array.</param>
	/// <param name="right">The second array.</param>
	/// <returns><see langword="true"/> if the arrays aren't equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
