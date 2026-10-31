namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// Represents a bounded region of a given span with an index and a length. This structure defines the starting index and the length of a segment within a span.
/// </summary>
[DebuggerStepThrough, StructLayout(LayoutKind.Sequential), DebuggerDisplay(@"\{Index={Index}, Length={Length}\}")]
public readonly struct SpanPosition : IComparable<SpanPosition>, IEquatable<int>, IEquatable<SpanPosition>
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly int _index, _length;

	/// <summary>
	/// The exclusive end index of the segment within a given span.
	/// </summary>
	public int End => _index + _length;
	/// <summary>
	/// The starting index of the segment within a given span.
	/// </summary>
	public int Index => _index;
	/// <summary>
	/// Indicates whether the segment is defined within a given span or represents an undefined region.
	/// </summary>
	public bool IsDefined => _index >= 0 && _length > 0;
	/// <summary>
	/// The length of the segment within a given span.
	/// </summary>
	public int Length => _length;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private SpanPosition(uint index, uint length)
	{
		_index = (int)index;
		_length = (int)length;
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="SpanPosition"/> struct with the specified start index and length.
	/// </summary>
	/// <param name="start">The starting index of the span. Must be greater than or equal to 0.</param>
	/// <param name="length">The length of the span segment. Must be greater than or equal to 0.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="start"/> is less than 0 or <paramref name="length"/> is negative.</exception>
	public SpanPosition(int start, int length)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(start);
		ArgumentOutOfRangeException.ThrowIfNegative(length);
		_index = start;
		_length = length;
	}

	/// <summary>
	/// Compares the current <see cref="SpanPosition"/> to another <see cref="SpanPosition"/> object.
	/// </summary>
	/// <param name="other">The <see cref="SpanPosition"/> to compare with.</param>
	/// <returns>An integer that indicates the relative position in the sort order.</returns>
	public int CompareTo(SpanPosition other)
	{
		int comparison = _index.CompareTo(other._index);
		return comparison == 0
			? _length.CompareTo(other._length)
			: comparison;
	}
	/// <summary>
	/// Determines whether the current <see cref="SpanPosition"/> is equal to the specified integer value representing a starting index.
	/// </summary>
	/// <param name="other">The integer value to compare with.</param>
	/// <returns><see langword="true"/> if the starting index is equal to <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
	public bool Equals(int other)
	{
		return _index == other;
	}
	/// <summary>
	/// Determines whether the current <see cref="SpanPosition"/> is equal to another <see cref="SpanPosition"/> object.
	/// </summary>
	/// <param name="other">The <see cref="SpanPosition"/> to compare with.</param>
	/// <returns><see langword="true"/> if both objects represent the same segment; otherwise, <see langword="false"/>.</returns>
	public bool Equals(SpanPosition other)
	{
		return _index == other._index && _length == other._length;
	}
	/// <inheritdoc/>
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return obj switch
		{
			SpanPosition pos => this.Equals(pos),
			int i => this.Equals(i),
			long longVal when longVal <= int.MaxValue && longVal >= int.MinValue => this.Equals((int)longVal),
			uint untInt when untInt <= int.MaxValue => this.Equals((int)untInt),
			ulong untLong when untLong <= int.MaxValue => this.Equals((int)untLong),
			_ => false,
		};
	}

	/// <summary>
	/// Serves as the default hash function for the current object.
	/// </summary>
	/// <returns>A hash code for the current object, suitable for use in hashing algorithms and data structures such as a hash
	/// table.</returns>
	public override int GetHashCode()
	{
		return HashCode.Combine(_index, _length);
	}

	/// <summary>
	/// Converts the current <see cref="SpanPosition"/> to a <see cref="Range"/> object.
	/// </summary>
	/// <returns>A <see cref="Range"/> object representing the span position and length.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Range ToRange()
	{
		return this.IsDefined ? new Range(_index, this.End) : new Range(0, 0);
	}

	/// <summary>
	/// Validates that the specified <see cref="SpanPosition"/> is within the bounds of the given length.
	/// </summary>
	/// <remarks>Use this method to ensure that a <see cref="SpanPosition"/> is valid within the context of a
	/// specified range.</remarks>
	/// <param name="position">The span position to validate, including its starting index and length.</param>
	/// <param name="length">The total length of the range against which the span position is validated.</param>
	/// <param name="paramName">The name of the parameter being validated. Automatically populated by the compiler if not explicitly provided.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="position"/> has a negative index, a negative length, or its calculated end exceeds
	/// <paramref name="length"/>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowIfOutOfRange(SpanPosition position, int length, [CallerArgumentExpression(nameof(position))] string? paramName = null)
	{
		// Comparing as unsigned values rejects a negative index or length, and the subtraction, unlike the sum of the
		// index and length, can't overflow once the index is known to lie within the range.
		if ((uint)position._index > (uint)length || (uint)position._length > (uint)(length - position._index))
		{
			throwEx(position, length, paramName);
		}

		[DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
		static void throwEx(SpanPosition position, int length, string? paramName)
		{
			throw new ArgumentOutOfRangeException(paramName, $"The span position {{Index={position._index}, Length={position._length}}} is out of range for the specified length {length}.");
		}
	}
	/// <summary>
	/// Throws an exception if the specified <see cref="SpanPosition"/> is undefined.
	/// </summary>
	/// <param name="position">The <see cref="SpanPosition"/> to validate. Must have a non-zero length.</param>
	/// <param name="paramName">The name of the parameter being validated. Automatically populated by the compiler if not explicitly provided.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="position"/> has a length of zero, indicating an undefined span position.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ThrowIfUndefined(SpanPosition position, [CallerArgumentExpression(nameof(position))] string? paramName = null)
	{
		if (position._length <= 0)
		{
			throwInvalid(position, paramName);
		}

		[DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
		static void throwInvalid(SpanPosition position, string? paramName)
		{
			throw new ArgumentException(
				paramName: paramName,
				message: "The span position is undefined (index or length is out of range).",
				innerException: new ArgumentOutOfRangeException(paramName, position._length, "Length is negative or zero."));
		}
	}

	/// <summary>
	/// Creates a new <see cref="SpanPosition"/> instance without performing any validation on the provided index and length.
	/// </summary>
	/// <param name="index">The starting index of the span.</param>
	/// <param name="length">The length of the span.</param>
	/// <returns>A new <see cref="SpanPosition"/> instance representing the specified span.</returns>
	internal static SpanPosition UnsafeCreate(int index, int length)
	{
		return new SpanPosition((uint)index, (uint)length);
	}

	/// <summary>
	/// Represents an undefined region of a span, where the index and length are both invalid.
	/// </summary>
	public static readonly SpanPosition Undefined = default;

	/// <summary>
	/// Determines whether two <see cref="SpanPosition"/> instances are equal.
	/// </summary>
	/// <param name="left">The first <see cref="SpanPosition"/> to compare.</param>
	/// <param name="right">The second <see cref="SpanPosition"/> to compare.</param>
	/// <returns><see langword="true"/> if both <see cref="SpanPosition"/> instances are equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(SpanPosition left, SpanPosition right)
	{
		return left._index == right._index && left._length == right._length;
	}
	/// <summary>
	/// Determines whether two <see cref="SpanPosition"/> instances are not equal.
	/// </summary>
	/// <param name="left">The first <see cref="SpanPosition"/> to compare.</param>
	/// <param name="right">The second <see cref="SpanPosition"/> to compare.</param>
	/// <returns><see langword="true"/> if both <see cref="SpanPosition"/> instances are not equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(SpanPosition left, SpanPosition right)
	{
		return left._index != right._index || left._length != right._length;
	}
	/// <summary>
	/// Converts a specified <see cref="SpanPosition"/> instance to a <see cref="Range"/> representing the same span.
	/// </summary>
	/// <remarks>Use this operator to obtain a <see cref="Range"/> that corresponds to the position represented by
	/// the given <see cref="SpanPosition"/>. The resulting range reflects the start and end points defined by the
	/// position.</remarks>
	/// <param name="position">The <see cref="SpanPosition"/> to convert to a <see cref="Range"/>.</param>
	public static explicit operator Range(SpanPosition position)
	{
		return position.ToRange();
	}
	/// <summary>
	/// Determines whether the specified left <see cref="SpanPosition"/> is greater than the specified right SpanPosition.
	/// </summary>
	/// <remarks>Comparison is based first on the index, and then on the length if the indexes are equal.</remarks>
	/// <param name="left">The first SpanPosition to compare.</param>
	/// <param name="right">The second SpanPosition to compare.</param>
	/// <returns>true if left is greater than right; otherwise, false.</returns>
	public static bool operator >(SpanPosition left, SpanPosition right)
	{
		return left._index != right._index ? left._index > right._index : left._length > right._length;
	}
	/// <summary>
	/// Determines whether the specified SpanPosition on the left is less than the SpanPosition on the right.
	/// </summary>
	/// <remarks>Comparison is based first on the index, and then on the length if the indices are equal.</remarks>
	/// <param name="left">The first SpanPosition to compare.</param>
	/// <param name="right">The second SpanPosition to compare.</param>
	/// <returns>true if the left SpanPosition is less than the right SpanPosition; otherwise, false.</returns>
	public static bool operator <(SpanPosition left, SpanPosition right)
	{
		return left._index != right._index ? left._index < right._index : left._length < right._length;
	}
	/// <summary>
	/// Determines whether the position represented by the left operand is greater than or equal to the position
	/// represented by the right operand.
	/// </summary>
	/// <param name="left">The first SpanPosition to compare.</param>
	/// <param name="right">The second SpanPosition to compare.</param>
	/// <returns>true if the left operand is greater than or equal to the right operand; otherwise, false.</returns>
	public static bool operator >=(SpanPosition left, SpanPosition right)
	{
		return !(left < right);
	}
	/// <summary>
	/// Determines whether the first SpanPosition is less than or equal to the second SpanPosition.
	/// </summary>
	/// <param name="left">The first SpanPosition to compare.</param>
	/// <param name="right">The second SpanPosition to compare.</param>
	/// <returns>true if left is less than or equal to right; otherwise, false.</returns>
	public static bool operator <=(SpanPosition left, SpanPosition right)
	{
		return !(left > right);
	}
}