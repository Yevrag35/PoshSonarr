namespace MG.Sonarr.Next.Enums;

/// <summary>
/// A ref struct that enumerates the mask of <typeparamref name="T"/> enumeration values.
/// </summary>
/// <typeparam name="T">The type of enumeration values to enumerate - must be decorated with the <see cref="FlagsAttribute"/>.</typeparam>
[DebuggerStepThrough]
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay(@"\{Current = {Current}, Count = {Count}\}")]
internal ref struct FlagEnumerator<T> where T : unmanaged, Enum
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private int _count;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private T _current;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private T _original;
	private int _flags;

	public readonly T Current => _current;
	internal readonly int Count => _count;
	internal readonly T Original => _original;

	internal FlagEnumerator(T flags)
	{
		_original = flags;
		ref int intFlag = ref Unsafe.As<T, int>(ref flags);
		_flags = intFlag;

		_count = 0;
		_current = default;
	}

	public readonly FlagEnumerator<T> GetEnumerator() => this;

	public bool MoveNext()
	{
		return this.MoveNextCore(stopEarly: 0 == _flags);
	}
	internal bool MoveNext(in bool flag)
	{
		return this.MoveNextCore(stopEarly: flag || 0 == _flags);
	}
	private bool MoveNextCore(bool stopEarly)
	{
		if (stopEarly)
		{
			return false;
		}

		int flagNum = _flags;
		int currentBit = flagNum & -flagNum; // isolate the rightmost bit
		_current = Unsafe.As<int, T>(ref currentBit);
		_count++;
		_flags &= ~currentBit;  // clear the rightmost bit
		return true;
	}

	internal void Reset()
	{
		int intFlags = Unsafe.As<T, int>(ref _original);  // Deliberately copying.
		_flags = intFlags;
	}
}