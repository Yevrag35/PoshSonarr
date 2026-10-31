using System.Numerics;

namespace MG.Sonarr.Next.Enums;

/// <summary>
/// Enumerate the individual set flags of an enum value.
/// </summary>
/// <remarks>
/// <para>This enumerator iterates over the set bits of a flags value by repeatedly isolating and clearing the lowest set bit.</para>
/// <para>Because this type is a <c>ref struct</c>, it is stack-only and cannot be boxed, captured by lambdas, or stored on the managed heap.</para>
/// <para>Type constraints: <typeparamref name="TEnum"/> is an unmanaged <see cref="Enum"/>, and <typeparamref name="TUnderlying"/> is an unmanaged integer type that supports binary operations.</para>
/// <para><b>Performance:</b> Uses bit-twiddling and does not allocate or use reflection.</para>
/// </remarks>
/// <typeparam name="TEnum">The enum type to enumerate flags for.</typeparam>
/// <typeparam name="TUnderlying">The underlying integer type used for bit operations.</typeparam>
[StructLayout(LayoutKind.Auto), DebuggerStepThrough]
public ref struct FlagEnumerator<TEnum, TUnderlying>
	where TEnum : unmanaged, Enum
	where TUnderlying : unmanaged, IBinaryInteger<TUnderlying>
{
	private readonly TUnderlying _original;
	private TUnderlying _flags;
	private TEnum _current;

	/// <summary>
	/// Get the current flag value.
	/// </summary>
	/// <remarks>
	/// <para>This value is undefined before the first successful call to <see cref="MoveNext()"/>.</para>
	/// </remarks>
	/// <value>The current flag value.</value>
	public readonly TEnum Current => _current;

	/// <summary>
	/// Initializes a new <see cref="FlagEnumerator{TEnum, TUnderlying}"/> instance for the specified flags value.
	/// </summary>
	/// <param name="flags">The flags value to enumerate.</param>
	/// <inheritdoc cref="ValidateSizesMatch" path="/exception"/>
	internal FlagEnumerator(TEnum flags)
	{
		ValidateSizesMatch();
		TUnderlying flagNum = Unsafe.As<TEnum, TUnderlying>(ref flags);
		_original = flagNum;
		_flags = flagNum;

		_current = default;
	}

	/// <exception cref="NotSupportedException">Thrown if the enumeration type does not have the same size as the underlying type.</exception>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ValidateSizesMatch()
	{
		if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TUnderlying>())
		{
			throwMismatchedSizes();
		}

		debugValidateIsEnumerable();

		[Conditional("DEBUG"), MethodImpl(MethodImplOptions.NoInlining)]
		static void debugValidateIsEnumerable()
		{
			if (!typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false))
			{
				throw new NotSupportedException($"The enumeration type '{typeof(TEnum).FullName}' does not support flag enumeration.");
			}

			if (!Enum.GetUnderlyingType(typeof(TEnum)).Equals(typeof(TUnderlying)))
			{
				throw new NotSupportedException($"The enumeration type '{typeof(TEnum).FullName}' does not have '{typeof(TUnderlying).FullName}' as its underlying type.");
			}
		};
		[StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
		static void throwMismatchedSizes()
		{
			int enumSize = Unsafe.SizeOf<TEnum>();
			int underlyingSize = Unsafe.SizeOf<TUnderlying>();

			throw new NotSupportedException(
				message: $"The enumeration type '{typeof(TEnum).FullName}' does not have the same size as the underlying type '{typeof(TUnderlying).FullName}' (Expected: '{enumSize}', Got: '{underlyingSize}')");
		};
	}

	/// <summary>
	/// Return an enumerator instance for use in a <c>foreach</c> statement.
	/// </summary>
	/// <returns>This enumerator instance.</returns>
	public readonly FlagEnumerator<TEnum, TUnderlying> GetEnumerator() => this;

	/// <summary>
	/// Advance the enumerator to the next set flag.
	/// </summary>
	/// <returns><see langword="true"/> if another flag was found; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
		if (TUnderlying.IsZero(_flags))
		{
			return false;
		}

		TUnderlying flagNum = _flags;
		TUnderlying currentBit = flagNum & -flagNum; // isolate the rightmost bit
		_current = Unsafe.As<TUnderlying, TEnum>(ref currentBit);
		_flags &= ~currentBit;  // clear the rightmost bit
		return true;
	}

	/// <summary>
	/// Reset the enumerator to its initial state.
	/// </summary>
	public void Reset()
	{
		_flags = _original;
		_current = default;
	}
}
