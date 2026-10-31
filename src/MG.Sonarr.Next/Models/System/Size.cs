namespace MG.Sonarr.Next.Models.System;

/// <summary>
/// Represents a number of bytes, with its size in mebibytes, gibibytes, and tebibytes.
/// </summary>
/// <remarks>
/// <para>
/// The sizes in <see cref="InMB"/>, <see cref="InGB"/>, and <see cref="InTB"/> divide the bytes by 1,024 to the power of
/// 2, 3, and 4, and round to two decimal places, or to the precision that the constructor is given, with midpoints
/// rounded away from zero. They're computed once, when the size is created.
/// </para>
/// <para>
/// A size compares and equates by its bytes alone, with another size or with an <see cref="int"/> or a
/// <see cref="long"/>.
/// </para>
/// </remarks>
public readonly struct Size : IComparable, IComparable<Size>, IEquatable<Size>,
	IComparable<int>,
	IComparable<long>,
	IEquatable<int>,
	IEquatable<long>
{
	/// <summary>
	/// The number of decimal places that the sizes round to by default.
	/// </summary>
	const int DEFAULT_PRECISION = 2;
	/// <summary>
	/// The number of bytes in a mebibyte.
	/// </summary>
	const double ONE_MB = 1_048_576d;
	/// <summary>
	/// The number of bytes in a gibibyte.
	/// </summary>
	const double ONE_GB = 1_073_741_824d;
	/// <summary>
	/// The number of bytes in a tebibyte.
	/// </summary>
	const double ONE_TB = 1_099_511_627_776d;

	/// <summary>
	/// The number of bytes.
	/// </summary>
	readonly long _bytes;
	/// <summary>
	/// The size in mebibytes, rounded.
	/// </summary>
	readonly double _inMb;
	/// <summary>
	/// The size in gibibytes, rounded.
	/// </summary>
	readonly double _inGb;
	/// <summary>
	/// The size in tebibytes, rounded.
	/// </summary>
	readonly double _inTb;

	/// <summary>
	/// Gets the size in mebibytes.
	/// </summary>
	/// <value>The number of bytes divided by 1,048,576, rounded.</value>
	public double InMB => _inMb;
	/// <summary>
	/// Gets the size in gibibytes.
	/// </summary>
	/// <value>The number of bytes divided by 1,073,741,824, rounded.</value>
	public double InGB => _inGb;
	/// <summary>
	/// Gets the size in tebibytes.
	/// </summary>
	/// <value>The number of bytes divided by 1,099,511,627,776, rounded.</value>
	public double InTB => _inTb;
	/// <summary>
	/// Gets the number of bytes.
	/// </summary>
	/// <value>The exact number of bytes.</value>
	public long TotalBytes => _bytes;

	/// <summary>
	/// Initializes a new <see cref="Size"/> instance from a number of bytes, with sizes rounded to two decimal places.
	/// </summary>
	/// <param name="bytes">The number of bytes.</param>
	public Size(in long bytes)
		: this(in bytes, DEFAULT_PRECISION)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="Size"/> instance from a number of bytes, with sizes rounded to the specified number
	/// of decimal places.
	/// </summary>
	/// <param name="bytes">The number of bytes.</param>
	/// <param name="roundingPrecision">The number of decimal places that the sizes round to, from 0 to 15.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="roundingPrecision"/> is less than 0 or greater than 15.
	/// </exception>
	public Size(in long bytes, in int roundingPrecision)
	{
		_bytes = bytes;
		_inMb = CalculateSize(in bytes, ONE_MB, in roundingPrecision);
		_inGb = CalculateSize(in bytes, ONE_GB, in roundingPrecision);
		_inTb = CalculateSize(in bytes, ONE_TB, in roundingPrecision);
	}

	/// <summary>
	/// Divides a number of bytes by a unit's size and rounds the result.
	/// </summary>
	/// <param name="bytes">The number of bytes.</param>
	/// <param name="divideBy">The number of bytes in the unit.</param>
	/// <param name="precision">The number of decimal places to round to.</param>
	/// <returns>The size in the unit, rounded with midpoints away from zero.</returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="precision"/> is less than 0 or greater than 15.
	/// </exception>
	private static double CalculateSize(in long bytes, double divideBy, in int precision)
	{
		double divide = bytes / divideBy;
		return Math.Round(divide, precision, MidpointRounding.AwayFromZero);
	}

	/// <summary>
	/// Compares this size with an object by bytes.
	/// </summary>
	/// <param name="other">The <see cref="Size"/>, <see cref="int"/>, or <see cref="long"/> to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this size is less than <paramref name="other"/>, 0 if they're equal, or a positive number
	/// if it's greater or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="other"/> isn't null, a <see cref="Size"/>, an <see cref="int"/>, or a
	/// <see cref="long"/>.
	/// </exception>
	public int CompareTo(object? other)
	{
		return other is int intVal
			? _bytes.CompareTo(intVal)
			: other is Size size
				? _bytes.CompareTo(size._bytes)
				: _bytes.CompareTo(other);
	}
	/// <summary>
	/// Compares this size with another by bytes.
	/// </summary>
	/// <param name="other">The size to compare with.</param>
	/// <returns>
	/// A negative number if this size is less than <paramref name="other"/>, 0 if they're equal, or a positive number
	/// if it's greater.
	/// </returns>
	public int CompareTo(Size other)
	{
		return this.CompareTo(other._bytes);
	}
	/// <summary>
	/// Compares this size with a number of bytes.
	/// </summary>
	/// <param name="other">The number of bytes to compare with.</param>
	/// <returns>
	/// A negative number if this size is less than <paramref name="other"/>, 0 if they're equal, or a positive number
	/// if it's greater.
	/// </returns>
	public int CompareTo(long other)
	{
		return _bytes.CompareTo(other);
	}
	/// <summary>
	/// Compares this size with a number of bytes.
	/// </summary>
	/// <param name="other">The number of bytes to compare with.</param>
	/// <returns>
	/// A negative number if this size is less than <paramref name="other"/>, 0 if they're equal, or a positive number
	/// if it's greater.
	/// </returns>
	public int CompareTo(int other)
	{
		return this.CompareTo((long)other);
	}
	/// <summary>
	/// Determines whether this size has the same number of bytes as another.
	/// </summary>
	/// <param name="other">The size to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if both sizes have the same number of bytes; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Equals(Size other)
	{
		return this.Equals(other._bytes);
	}
	/// <summary>
	/// Determines whether this size is a number of bytes.
	/// </summary>
	/// <param name="other">The number of bytes to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if this size has <paramref name="other"/> bytes; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Equals(int other)
	{
		return _bytes == other;
	}
	/// <summary>
	/// Determines whether this size is a number of bytes.
	/// </summary>
	/// <param name="other">The number of bytes to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if this size has <paramref name="other"/> bytes; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Equals(long other)
	{
		return _bytes == other;
	}
	/// <summary>
	/// Determines whether this size equals an object by bytes.
	/// </summary>
	/// <param name="obj">The object to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="obj"/> is a <see cref="Size"/> with the same number of bytes, or an
	/// <see cref="int"/> or a <see cref="long"/> equal to them; otherwise, <see langword="false"/>.
	/// </returns>
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Size size)
		{
			return this.Equals(size);
		}
		else if (obj is long || obj is int)
		{
			return this.Equals((long)obj);
		}

		return false;
	}
	/// <summary>
	/// Returns a hash code for the size.
	/// </summary>
	/// <returns>The hash code of the number of bytes.</returns>
	public override int GetHashCode()
	{
		return _bytes.GetHashCode();
	}

	/// <summary>
	/// Converts a number of bytes to a size.
	/// </summary>
	/// <param name="bytes">The number of bytes.</param>
	/// <returns>A size of <paramref name="bytes"/> bytes.</returns>
	public static implicit operator Size(int bytes)
	{
		return new(bytes);
	}
	/// <summary>
	/// Converts a number of bytes to a size.
	/// </summary>
	/// <param name="bytes">The number of bytes.</param>
	/// <returns>A size of <paramref name="bytes"/> bytes.</returns>
	public static explicit operator Size(long bytes)
	{
		return new(in bytes);
	}

	/// <summary>
	/// Determines whether two sizes have the same number of bytes.
	/// </summary>
	/// <param name="x">The first size.</param>
	/// <param name="y">The second size.</param>
	/// <returns><see langword="true"/> if the sizes are equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(Size x, Size y)
	{
		return x.Equals(y);
	}
	/// <summary>
	/// Determines whether two sizes have different numbers of bytes.
	/// </summary>
	/// <param name="x">The first size.</param>
	/// <param name="y">The second size.</param>
	/// <returns><see langword="true"/> if the sizes aren't equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(Size x, Size y)
	{
		return !(x == y);
	}
	/// <summary>
	/// Determines whether a size is a number of bytes.
	/// </summary>
	/// <param name="x">The size.</param>
	/// <param name="y">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="x"/> has <paramref name="y"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(Size x, long y)
	{
		return x.Equals(y);
	}
	/// <summary>
	/// Determines whether a size isn't a number of bytes.
	/// </summary>
	/// <param name="x">The size.</param>
	/// <param name="y">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="x"/> doesn't have <paramref name="y"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(Size x, long y)
	{
		return !(x == y);
	}
	/// <summary>
	/// Determines whether a number of bytes is a size.
	/// </summary>
	/// <param name="x">The number of bytes.</param>
	/// <param name="y">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="y"/> has <paramref name="x"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(long x, Size y)
	{
		return y.Equals(x);
	}
	/// <summary>
	/// Determines whether a number of bytes isn't a size.
	/// </summary>
	/// <param name="x">The number of bytes.</param>
	/// <param name="y">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="y"/> doesn't have <paramref name="x"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(long x, Size y)
	{
		return !(x == y);
	}
	/// <summary>
	/// Determines whether a size is a number of bytes.
	/// </summary>
	/// <param name="x">The size.</param>
	/// <param name="y">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="x"/> has <paramref name="y"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(Size x, int y)
	{
		return x.Equals(y);
	}
	/// <summary>
	/// Determines whether a size isn't a number of bytes.
	/// </summary>
	/// <param name="x">The size.</param>
	/// <param name="y">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="x"/> doesn't have <paramref name="y"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(Size x, int y)
	{
		return !(x == y);
	}
	/// <summary>
	/// Determines whether a number of bytes is a size.
	/// </summary>
	/// <param name="x">The number of bytes.</param>
	/// <param name="y">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="y"/> has <paramref name="x"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(int x, Size y)
	{
		return y.Equals(x);
	}
	/// <summary>
	/// Determines whether a number of bytes isn't a size.
	/// </summary>
	/// <param name="x">The number of bytes.</param>
	/// <param name="y">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="y"/> doesn't have <paramref name="x"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(int x, Size y)
	{
		return !(x == y);
	}

	/// <summary>
	/// Determines whether a size is smaller than another.
	/// </summary>
	/// <param name="left">The first size.</param>
	/// <param name="right">The second size.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has fewer bytes than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	public static bool operator <(Size left, Size right)
	{
		return left.CompareTo(right) < 0;
	}
	/// <summary>
	/// Determines whether a size is smaller than or equal to another.
	/// </summary>
	/// <param name="left">The first size.</param>
	/// <param name="right">The second size.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has no more bytes than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	public static bool operator <=(Size left, Size right)
	{
		return left.CompareTo(right) <= 0;
	}
	/// <summary>
	/// Determines whether a size is larger than another.
	/// </summary>
	/// <param name="left">The first size.</param>
	/// <param name="right">The second size.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has more bytes than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	public static bool operator >(Size left, Size right)
	{
		return left.CompareTo(right) > 0;
	}
	/// <summary>
	/// Determines whether a size is larger than or equal to another.
	/// </summary>
	/// <param name="left">The first size.</param>
	/// <param name="right">The second size.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has no fewer bytes than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	public static bool operator >=(Size left, Size right)
	{
		return left.CompareTo(right) >= 0;
	}

	/// <summary>
	/// Determines whether a size is smaller than a number of bytes.
	/// </summary>
	/// <param name="left">The size.</param>
	/// <param name="right">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has fewer than <paramref name="right"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator <(Size left, int right)
	{
		return left.CompareTo(right) < 0;
	}
	/// <summary>
	/// Determines whether a size is smaller than or equal to a number of bytes.
	/// </summary>
	/// <param name="left">The size.</param>
	/// <param name="right">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has no more than <paramref name="right"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator <=(Size left, int right)
	{
		return left.CompareTo(right) <= 0;
	}
	/// <summary>
	/// Determines whether a size is larger than a number of bytes.
	/// </summary>
	/// <param name="left">The size.</param>
	/// <param name="right">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has more than <paramref name="right"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator >(Size left, int right)
	{
		return left.CompareTo(right) > 0;
	}
	/// <summary>
	/// Determines whether a size is larger than or equal to a number of bytes.
	/// </summary>
	/// <param name="left">The size.</param>
	/// <param name="right">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has no fewer than <paramref name="right"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator >=(Size left, int right)
	{
		return left.CompareTo(right) >= 0;
	}
	/// <summary>
	/// Determines whether a number of bytes is smaller than a size.
	/// </summary>
	/// <param name="left">The number of bytes.</param>
	/// <param name="right">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="right"/> has more than <paramref name="left"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator <(int left, Size right)
	{
		return ((long)left).CompareTo(right._bytes) < 0;
	}
	/// <summary>
	/// Determines whether a number of bytes is smaller than or equal to a size.
	/// </summary>
	/// <param name="left">The number of bytes.</param>
	/// <param name="right">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="right"/> has no fewer than <paramref name="left"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator <=(int left, Size right)
	{
		return ((long)left).CompareTo(right._bytes) <= 0;
	}
	/// <summary>
	/// Determines whether a number of bytes is larger than a size.
	/// </summary>
	/// <param name="left">The number of bytes.</param>
	/// <param name="right">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="right"/> has fewer than <paramref name="left"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator >(int left, Size right)
	{
		return ((long)left).CompareTo(right._bytes) > 0;
	}
	/// <summary>
	/// Determines whether a number of bytes is larger than or equal to a size.
	/// </summary>
	/// <param name="left">The number of bytes.</param>
	/// <param name="right">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="right"/> has no more than <paramref name="left"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator >=(int left, Size right)
	{
		return ((long)left).CompareTo(right._bytes) >= 0;
	}

	/// <summary>
	/// Determines whether a size is smaller than a number of bytes.
	/// </summary>
	/// <param name="left">The size.</param>
	/// <param name="right">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has fewer than <paramref name="right"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator <(Size left, long right)
	{
		return left.CompareTo(right) < 0;
	}
	/// <summary>
	/// Determines whether a size is smaller than or equal to a number of bytes.
	/// </summary>
	/// <param name="left">The size.</param>
	/// <param name="right">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has no more than <paramref name="right"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator <=(Size left, long right)
	{
		return left.CompareTo(right) <= 0;
	}
	/// <summary>
	/// Determines whether a size is larger than a number of bytes.
	/// </summary>
	/// <param name="left">The size.</param>
	/// <param name="right">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has more than <paramref name="right"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator >(Size left, long right)
	{
		return left.CompareTo(right) > 0;
	}
	/// <summary>
	/// Determines whether a size is larger than or equal to a number of bytes.
	/// </summary>
	/// <param name="left">The size.</param>
	/// <param name="right">The number of bytes.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> has no fewer than <paramref name="right"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator >=(Size left, long right)
	{
		return left.CompareTo(right) >= 0;
	}
	/// <summary>
	/// Determines whether a number of bytes is smaller than a size.
	/// </summary>
	/// <param name="left">The number of bytes.</param>
	/// <param name="right">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="right"/> has more than <paramref name="left"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator <(long left, Size right)
	{
		return left.CompareTo(right._bytes) < 0;
	}
	/// <summary>
	/// Determines whether a number of bytes is smaller than or equal to a size.
	/// </summary>
	/// <param name="left">The number of bytes.</param>
	/// <param name="right">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="right"/> has no fewer than <paramref name="left"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator <=(long left, Size right)
	{
		return left.CompareTo(right._bytes) <= 0;
	}
	/// <summary>
	/// Determines whether a number of bytes is larger than a size.
	/// </summary>
	/// <param name="left">The number of bytes.</param>
	/// <param name="right">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="right"/> has fewer than <paramref name="left"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator >(long left, Size right)
	{
		return left.CompareTo(right._bytes) > 0;
	}
	/// <summary>
	/// Determines whether a number of bytes is larger than or equal to a size.
	/// </summary>
	/// <param name="left">The number of bytes.</param>
	/// <param name="right">The size.</param>
	/// <returns><see langword="true"/> if <paramref name="right"/> has no more than <paramref name="left"/> bytes; otherwise, <see langword="false"/>.</returns>
	public static bool operator >=(long left, Size right)
	{
		return left.CompareTo(right._bytes) >= 0;
	}
}

