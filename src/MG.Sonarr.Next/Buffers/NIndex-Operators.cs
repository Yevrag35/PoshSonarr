namespace MG.Sonarr.Next.Buffers;

public readonly partial struct NIndex
{
	// UINT MATH
	//public static NIndex operator +(NIndex left, uint right)
	//{
	//	// Mask the uint to wrap it around at int.MaxValue
	//	//uint wrappedRight = right & int.MaxValue;
	//	int wrappedRight = WrapUIntAndCast(ref right);

	//	// Add the left value to the masked uint and wrap the result within [0, int.MaxValue]
	//	int result = (left._data + wrappedRight) & int.MaxValue;

	//	return new NIndex(result);
	//}
	public static NIndex operator ++(NIndex index)
	{
		return index.Increment();
	}

	public static uint operator +(NIndex left, uint right)
	{
		// Mask the uint to wrap it around at int.MaxValue
		int wrappedRight = WrapUIntAndCast(ref right);

		// Add the masked uint to the left value and wrap the result within [0, int.MaxValue]
		uint result = (uint)((left._data + wrappedRight) & int.MaxValue);

		return result;
	}
	[DebuggerStepThrough]
	public static uint operator +(uint left, NIndex right)
	{
		return right + left;
	}

	// CHAR ADDITION
	[DebuggerStepThrough]
	public static char operator +(NIndex left, char right)
	{
		return (char)(right + left._data);
	}
	[DebuggerStepThrough]
	public static char operator +(char left, NIndex right)
	{
		return (char)(left + right._data);
	}

	public static NIndex operator -(NIndex x, int y)
	{
		return new(x._data - y);
	}
	// CASTING OPERATORS
	[DebuggerStepThrough]
	public static explicit operator NIndex(int value) => new(value);
	[DebuggerStepThrough]
	public static implicit operator NIndex(uint value) => new(value);
	public static implicit operator NIndex(ushort value) => new(value, isUnchecked: true);
	//public static explicit operator NIndex(ReadOnlySpan<char> span) => new(span.Length, isUnchecked: true);
	public static explicit operator NIndex(ReadOnlySpan<string> span) => new(span.Length, isUnchecked: true);
	[DebuggerStepThrough]
	public static explicit operator byte(NIndex index) => (byte)index._data;
	[DebuggerStepThrough]
	public static implicit operator int(NIndex index) => index._data;
	[DebuggerStepThrough]
	public static implicit operator long(NIndex index) => index._data;
	[DebuggerStepThrough]
	public static explicit operator uint(NIndex index) => (uint)index._data;
	[DebuggerStepThrough]
	public static explicit operator ulong(NIndex index) => (ulong)index._data;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int WrapUIntAndCast(ref uint value)
	{
		value &= int.MaxValue;
		return (int)value;
	}
}