namespace MG.Sonarr.Next.Numbers;

#pragma warning disable IDE0001
/// <summary>
/// Represents an "always-positive" 32-bit signed integer.
/// </summary>
/// <remarks>
/// Valid range is: <code>0 to 2147483647</code>
/// <para>
/// Conversions to <see cref="PInt32"/> wrap around the valid range: <c>-1</c> converts to <see cref="MaxValue"/>.
/// </para>
/// <para>
/// The addition, subtraction, multiplication, division, and remainder operators return an <see cref="int"/> computed in an
/// <see langword="unchecked"/> context, even inside a <see langword="checked"/> expression, so a result can fall outside the valid range:
/// subtracting <c>1</c> from a value of <see cref="MinValue"/> produces <c>-1</c>. Converting a result back to <see cref="PInt32"/> wraps
/// it, and for an expression built only from addition, subtraction, and multiplication, wrapping the final result produces the same value
/// as wrapping after every operation.
/// </para>
/// <para>
/// Addition, subtraction, and multiplication also take an <see cref="int"/> operand directly, without wrapping it. Division and remainder
/// take only <see cref="PInt32"/> operands, so an <see cref="int"/> operand wraps before the operation; to divide an unwrapped
/// <see cref="int"/>, convert the <see cref="PInt32"/> operand to <see cref="int"/> so the built-in <see cref="int"/> operator applies.
/// </para>
/// <para>
/// The increment and decrement operators return <see cref="PInt32"/> and wrap: incrementing a value of <see cref="MaxValue"/> produces
/// <see cref="MinValue"/>.
/// </para>
/// </remarks>
[DebuggerStepThrough, DebuggerDisplay("{_value,nq}")]
[SuppressMessage("Style", "IDE0023"), SuppressMessage("Style", "IDE0024")]
public readonly struct PInt32 : IComparable<PInt32>, IEquatable<PInt32>
{
	/// <summary>
	/// Represents the smallest possible value of <see cref="PInt32"/>.
	/// </summary>
	public const int MinValue = 0;
	/// <summary>
	/// Represents the largest possible value of <see cref="PInt32"/>.
	/// </summary>
	public const int MaxValue = int.MaxValue;

	private readonly int _value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private PInt32(int value)
	{
		_value = value & MaxValue;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private PInt32(uint uValue)
	{
		_value = (int)(uValue & MaxValue);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal PInt32(int value, bool _)
	{
		Debug.Assert(value is >= MinValue and <= MaxValue, $"Value {value} is out of range for PInt32.");
		_value = value;
	}

	/// <summary>
	/// Clamps an <see cref="int"/> value to the valid range of <see cref="PInt32"/>.
	/// </summary>
	/// <param name="n">The value to clamp.</param>
	/// <returns>A <see cref="PInt32"/> representing the clamped value.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PInt32 Clamp(int n)
	{
		return new(Math.Clamp(n, MinValue, MaxValue), _: true);
	}
	/// <summary>
	/// Clamps an <see cref="int"/> value to the valid range of <see cref="PInt32"/>.
	/// </summary>
	/// <param name="n">The value to clamp.</param>
	/// <returns>A <see cref="PInt32"/> representing the clamped value.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PInt32 Clamp(uint n)
	{
		return new((int)Math.Clamp(n, MinValue, MaxValue), _: true);
	}

	/// <summary>
	/// Compares this value to another <see cref="PInt32"/> value.
	/// </summary>
	/// <param name="other">The value to compare against this instance.</param>
	/// <returns>
	/// A value less than zero when this instance is less than <paramref name="other"/>,
	/// zero when they are equal, or a value greater than zero when this instance is greater than <paramref name="other"/>.
	/// </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(PInt32 other)
	{
		return _value.CompareTo(other._value);
	}

	/// <summary>
	/// Determines whether this instance and another <see cref="PInt32"/> instance represent the same value.
	/// </summary>
	/// <param name="other">The value to compare against this instance.</param>
	/// <returns><see langword="true"/> when the values are equal; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(PInt32 other)
	{
		return _value == other._value;
	}

	/// <summary>
	/// Determines whether this instance and a specified object represent the same value.
	/// </summary>
	/// <param name="obj">The object to compare with this instance.</param>
	/// <returns><see langword="true"/> when <paramref name="obj"/> represents the same value; otherwise, <see langword="false"/>.</returns>
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return obj switch
		{
			PInt32 p32 => this.Equals(p32),
			int i => _value == i,
			uint u => _value == u,
			byte b => _value == b,
			sbyte sb => _value == sb,
			ushort us => _value == us,
			short s => _value == s,
			long l => _value == l,
			float f => _value == f,
			decimal d => _value == d,
			double d => _value == d,
			ulong ul => (ulong)_value == ul,
			_ => false,
		};
	}
	// Equality and HashCode
	/// <summary>
	/// Returns the hash code for this instance.
	/// </summary>
	/// <returns>The hash code for this instance.</returns>
	public override int GetHashCode()
	{
		return _value.GetHashCode();
	}

	/// <summary>
	/// Returns a reference to the underlying value, which can be read in a volatile context.
	/// </summary>
	/// <returns>A reference to the underlying value.</returns>
	public int VolatileRead()
	{
		return Volatile.Read(in _value);
	}

	// Operators

	/// <summary>
	/// Adds two <see cref="PInt32"/> values.
	/// </summary>
	/// <param name="left">The first value.</param>
	/// <param name="right">The second value.</param>
	/// <returns>The sum of <paramref name="left"/> and <paramref name="right"/>, computed in an <see langword="unchecked"/> context and negative when the sum exceeds <see cref="MaxValue"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int operator +(PInt32 left, PInt32 right) => unchecked(left._value + right._value);
	/// <summary>
	/// Subtracts one <see cref="PInt32"/> value from another.
	/// </summary>
	/// <param name="left">The value to subtract from.</param>
	/// <param name="right">The value to subtract.</param>
	/// <returns>The difference of <paramref name="left"/> and <paramref name="right"/>, which is negative when <paramref name="right"/> is greater than <paramref name="left"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int operator -(PInt32 left, PInt32 right) => unchecked(left._value - right._value);
	/// <summary>
	/// Multiplies two <see cref="PInt32"/> values.
	/// </summary>
	/// <param name="left">The first value.</param>
	/// <param name="right">The second value.</param>
	/// <returns>The product of <paramref name="left"/> and <paramref name="right"/>, computed in an <see langword="unchecked"/> context.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int operator *(PInt32 left, PInt32 right) => unchecked(left._value * right._value);
	/// <summary>
	/// Divides one <see cref="PInt32"/> value by another.
	/// </summary>
	/// <remarks>
	/// An <see cref="int"/> operand converts to <see cref="PInt32"/>, and therefore wraps, before the division. To divide an unwrapped
	/// <see cref="int"/>, convert the <see cref="PInt32"/> operand to <see cref="int"/> so the built-in <see cref="int"/> operator applies.
	/// </remarks>
	/// <param name="left">The dividend.</param>
	/// <param name="right">The divisor.</param>
	/// <returns>The quotient of <paramref name="left"/> divided by <paramref name="right"/>, which is always within the valid range.</returns>
	/// <exception cref="DivideByZeroException">Thrown when <paramref name="right"/> is zero.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int operator /(PInt32 left, PInt32 right) => left._value / right._value;
	/// <summary>
	/// Computes the remainder of one <see cref="PInt32"/> value divided by another.
	/// </summary>
	/// <remarks>
	/// An <see cref="int"/> operand converts to <see cref="PInt32"/>, and therefore wraps, before the operation. To compute the remainder of
	/// an unwrapped <see cref="int"/>, convert the <see cref="PInt32"/> operand to <see cref="int"/> so the built-in <see cref="int"/>
	/// operator applies.
	/// </remarks>
	/// <param name="left">The dividend.</param>
	/// <param name="right">The divisor.</param>
	/// <returns>The remainder of <paramref name="left"/> divided by <paramref name="right"/>, which is always within the valid range.</returns>
	/// <exception cref="DivideByZeroException">Thrown when <paramref name="right"/> is zero.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int operator %(PInt32 left, PInt32 right) => left._value % right._value;

	/// <summary>
	/// Adds an <see cref="int"/> value to a <see cref="PInt32"/> value.
	/// </summary>
	/// <param name="left">The <see cref="PInt32"/> value.</param>
	/// <param name="right">The <see cref="int"/> value to add, used without wrapping.</param>
	/// <returns>The sum of <paramref name="left"/> and <paramref name="right"/>, computed in an <see langword="unchecked"/> context.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int operator +(PInt32 left, int right) => unchecked(left._value + right);
	/// <summary>
	/// Subtracts an <see cref="int"/> value from a <see cref="PInt32"/> value.
	/// </summary>
	/// <param name="left">The <see cref="PInt32"/> value to subtract from.</param>
	/// <param name="right">The <see cref="int"/> value to subtract, used without wrapping.</param>
	/// <returns>The difference of <paramref name="left"/> and <paramref name="right"/>, computed in an <see langword="unchecked"/> context.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int operator -(PInt32 left, int right) => unchecked(left._value - right);
	/// <summary>
	/// Multiplies a <see cref="PInt32"/> value by an <see cref="int"/> value.
	/// </summary>
	/// <param name="left">The <see cref="PInt32"/> value.</param>
	/// <param name="right">The <see cref="int"/> value to multiply by, used without wrapping.</param>
	/// <returns>The product of <paramref name="left"/> and <paramref name="right"/>, computed in an <see langword="unchecked"/> context.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int operator *(PInt32 left, int right) => unchecked(left._value * right);

	/// <summary>
	/// Adds a <see cref="PInt32"/> value to an <see cref="int"/> value.
	/// </summary>
	/// <param name="left">The <see cref="int"/> value, used without wrapping.</param>
	/// <param name="right">The <see cref="PInt32"/> value to add.</param>
	/// <returns>The sum of <paramref name="left"/> and <paramref name="right"/>, computed in an <see langword="unchecked"/> context.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int operator +(int left, PInt32 right) => unchecked(left + right._value);
	/// <summary>
	/// Subtracts a <see cref="PInt32"/> value from an <see cref="int"/> value.
	/// </summary>
	/// <param name="left">The <see cref="int"/> value to subtract from, used without wrapping.</param>
	/// <param name="right">The <see cref="PInt32"/> value to subtract.</param>
	/// <returns>The difference of <paramref name="left"/> and <paramref name="right"/>, computed in an <see langword="unchecked"/> context.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int operator -(int left, PInt32 right) => unchecked(left - right._value);
	/// <summary>
	/// Multiplies an <see cref="int"/> value by a <see cref="PInt32"/> value.
	/// </summary>
	/// <param name="left">The <see cref="int"/> value, used without wrapping.</param>
	/// <param name="right">The <see cref="PInt32"/> value to multiply by.</param>
	/// <returns>The product of <paramref name="left"/> and <paramref name="right"/>, computed in an <see langword="unchecked"/> context.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int operator *(int left, PInt32 right) => unchecked(left * right._value);

	/// <summary>
	/// Increments the value by one.
	/// </summary>
	/// <param name="value">The value to increment.</param>
	/// <returns>The incremented value, which wraps from <see cref="MaxValue"/> to <see cref="MinValue"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PInt32 operator ++(PInt32 value) => new(unchecked(value._value + 1));
	/// <summary>
	/// Decrements the value by one.
	/// </summary>
	/// <param name="value">The value to decrement.</param>
	/// <returns>The decremented value, which wraps from <see cref="MinValue"/> to <see cref="MaxValue"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PInt32 operator --(PInt32 value) => new(unchecked(value._value - 1));

	/// <summary>
	/// Determines whether two <see cref="PInt32"/> values are equal.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(PInt32 left, PInt32 right) => left._value == right._value;
	/// <summary>
	/// Determines whether two <see cref="PInt32"/> values are not equal.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(PInt32 left, PInt32 right) => left._value != right._value;
	/// <summary>
	/// Determines whether one <see cref="PInt32"/> value is less than another.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(PInt32 left, PInt32 right) => left._value < right._value;
	/// <summary>
	/// Determines whether one <see cref="PInt32"/> value is greater than another.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(PInt32 left, PInt32 right) => left._value > right._value;
	/// <summary>
	/// Determines whether one <see cref="PInt32"/> value is less than or equal to another.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(PInt32 left, PInt32 right) => left._value <= right._value;
	/// <summary>
	/// Determines whether one <see cref="PInt32"/> value is greater than or equal to another.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(PInt32 left, PInt32 right) => left._value >= right._value;

	/// <summary>
	/// Determines whether a <see cref="PInt32"/> value equals an <see cref="int"/> value.
	/// </summary>
	/// <param name="left">The <see cref="PInt32"/> value to compare.</param>
	/// <param name="right">The <see cref="int"/> value to compare.</param>
	/// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(PInt32 left, int right) => left._value == right;
	/// <summary>
	/// Determines whether a <see cref="PInt32"/> value does not equal an <see cref="int"/> value.
	/// </summary>
	/// <param name="left">The <see cref="PInt32"/> value to compare.</param>
	/// <param name="right">The <see cref="int"/> value to compare.</param>
	/// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(PInt32 left, int right) => left._value != right;
	/// <summary>
	/// Determines whether a <see cref="PInt32"/> value is less than an <see cref="int"/> value.
	/// </summary>
	/// <param name="left">The <see cref="PInt32"/> value to compare.</param>
	/// <param name="right">The <see cref="int"/> value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(PInt32 left, int right) => left._value < right;
	/// <summary>
	/// Determines whether a <see cref="PInt32"/> value is greater than an <see cref="int"/> value.
	/// </summary>
	/// <param name="left">The <see cref="PInt32"/> value to compare.</param>
	/// <param name="right">The <see cref="int"/> value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(PInt32 left, int right) => left._value > right;
	/// <summary>
	/// Determines whether a <see cref="PInt32"/> value is less than or equal to an <see cref="int"/> value.
	/// </summary>
	/// <param name="left">The <see cref="PInt32"/> value to compare.</param>
	/// <param name="right">The <see cref="int"/> value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(PInt32 left, int right) => left._value <= right;
	/// <summary>
	/// Determines whether a <see cref="PInt32"/> value is greater than or equal to an <see cref="int"/> value.
	/// </summary>
	/// <param name="left">The <see cref="PInt32"/> value to compare.</param>
	/// <param name="right">The <see cref="int"/> value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(PInt32 left, int right) => left._value >= right;

	/// <summary>
	/// Determines whether an <see cref="int"/> value equals a <see cref="PInt32"/> value.
	/// </summary>
	/// <param name="left">The <see cref="int"/> value to compare.</param>
	/// <param name="right">The <see cref="PInt32"/> value to compare.</param>
	/// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(int left, PInt32 right) => left == right._value;
	/// <summary>
	/// Determines whether an <see cref="int"/> value does not equal a <see cref="PInt32"/> value.
	/// </summary>
	/// <param name="left">The <see cref="int"/> value to compare.</param>
	/// <param name="right">The <see cref="PInt32"/> value to compare.</param>
	/// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(int left, PInt32 right) => left != right._value;
	/// <summary>
	/// Determines whether an <see cref="int"/> value is less than a <see cref="PInt32"/> value.
	/// </summary>
	/// <param name="left">The <see cref="int"/> value to compare.</param>
	/// <param name="right">The <see cref="PInt32"/> value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(int left, PInt32 right) => left < right._value;
	/// <summary>
	/// Determines whether an <see cref="int"/> value is greater than a <see cref="PInt32"/> value.
	/// </summary>
	/// <param name="left">The <see cref="int"/> value to compare.</param>
	/// <param name="right">The <see cref="PInt32"/> value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(int left, PInt32 right) => left > right._value;
	/// <summary>
	/// Determines whether an <see cref="int"/> value is less than or equal to a <see cref="PInt32"/> value.
	/// </summary>
	/// <param name="left">The <see cref="int"/> value to compare.</param>
	/// <param name="right">The <see cref="PInt32"/> value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(int left, PInt32 right) => left <= right._value;
	/// <summary>
	/// Determines whether an <see cref="int"/> value is greater than or equal to a <see cref="PInt32"/> value.
	/// </summary>
	/// <param name="left">The <see cref="int"/> value to compare.</param>
	/// <param name="right">The <see cref="PInt32"/> value to compare.</param>
	/// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(int left, PInt32 right) => left >= right._value;

	/// <summary>
	/// Implicitly converts an <see cref="int"/> value to <see cref="PInt32"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>A <see cref="PInt32"/> that represents <paramref name="value"/>, wrapped around the valid range; for example, <c>-1</c> converts to <see cref="MaxValue"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PInt32(int value) => new(value);
	/// <summary>
	/// Implicitly converts a <see cref="uint"/> value to <see cref="PInt32"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>A <see cref="PInt32"/> that represents <paramref name="value"/>, wrapped around the valid range.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PInt32(uint value) => new(value);
	/// <summary>
	/// Implicitly converts a <see cref="PInt32"/> value to <see cref="int"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator int(PInt32 value) => value._value;
	/// <summary>
	/// Explicitly converts a <see cref="PInt32"/> value to <see cref="uint"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator uint(PInt32 value) => (uint)value._value;
	/// <summary>
	/// Implicitly converts a <see cref="PInt32"/> value to <see cref="ulong"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ulong(PInt32 value) => (ulong)value._value;
	/// <summary>
	/// Explicitly converts a <see cref="PInt32"/> value to <see cref="ushort"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator ushort(PInt32 value) => (ushort)value._value;

	/// <summary>
	/// Returns the string representation of this value.
	/// </summary>
	/// <returns>The underlying value formatted as a string.</returns>
	public override string ToString()
	{
		return _value.ToString();
	}
}

#pragma warning restore
