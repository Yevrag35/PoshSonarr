using System.Numerics;

namespace MG.Sonarr.Next.Enums;

/// <summary>
/// Provides extension methods for enumerating individual flag values set in bit field enumerations.
/// </summary>
/// <remarks>These methods are intended for use with enumeration types that represent bit fields, typically those
/// marked with the <see cref="FlagsAttribute"/>. They enable efficient iteration over each flag that is set in a given enumeration
/// value, supporting various underlying integral types.</remarks>
public static class FlagEnumerationExtensions
{
	/// <summary>
	/// Returns an enumerator that iterates through all individual flag values set in the specified enumeration value.
	/// </summary>
	/// <remarks>This method is intended for use with bit field enumerations, typically those marked with the
	/// [Flags] attribute. The returned enumerator yields each individual flag that is set in the provided value, in order
	/// from least significant to most significant bit.</remarks>
	/// <typeparam name="TEnum">The enumeration type to enumerate flags for. Must be an unmanaged enum.</typeparam>
	/// <typeparam name="TUnderlying">The underlying integral type of the enumeration. Must implement IBinaryInteger.</typeparam>
	/// <param name="value">The enumeration value whose set flags are to be enumerated.</param>
	/// <returns>An enumerator that yields each flag value set in the specified enumeration value.</returns>
	/// <exception cref="NotSupportedException">Thrown if the enumeration type does not support flag enumeration or if the underlying type does not match.</exception>
	public static FlagEnumerator<TEnum, TUnderlying> EnumerateFlags<TEnum, TUnderlying>(this TEnum value)
		where TEnum : unmanaged, Enum
		where TUnderlying : unmanaged, IBinaryInteger<TUnderlying>
	{
		return new FlagEnumerator<TEnum, TUnderlying>(value);
	}
	/// <summary>
	/// Enumerates the individual flag values that are set in the specified enumeration value, using the underlying type
	/// <see cref="int"/>.
	/// </summary>
	/// <remarks>This method is intended for use with enumeration types that have <see cref="int"/> as their
	/// underlying type. If <typeparamref name="TEnum"/> has a different underlying type, use the appropriate
	/// overload.</remarks>
	/// <typeparam name="TEnum">The enumeration type to enumerate flags for. Must be an unmanaged enumeration.</typeparam>
	/// <param name="value">The enumeration value whose set flags are to be enumerated.</param>
	/// <returns>An enumerator that iterates through each flag of type <typeparamref name="TEnum"/> that is set in <paramref
	/// name="value"/>.</returns>
	/// <inheritdoc cref="EnumerateFlags{TEnum, TUnderlying}(TEnum)" path="/exception"/>
	[DebuggerStepThrough]
	public static FlagEnumerator<TEnum, int> EnumerateIntFlags<TEnum>(this TEnum value) where TEnum : unmanaged, Enum
	{
		return EnumerateFlags<TEnum, int>(value);
	}
	/// <summary>
	/// Enumerates the individual flag values that are set in a 16-bit unsigned enumeration value.
	/// </summary>
	/// <remarks>This method is intended for use with enums that have an underlying type of <see cref="ushort"/>. If used with
	/// enums of a different underlying type, only the lower 16 bits will be considered.</remarks>
	/// <typeparam name="TEnum">The enumeration type to inspect. Must be an unmanaged enum.</typeparam>
	/// <param name="value">The enumeration value whose set flags are to be enumerated.</param>
	/// <returns>An enumerator that yields each flag of type TEnum that is set in the specified value.</returns>
	/// <inheritdoc cref="EnumerateFlags{TEnum, TUnderlying}(TEnum)" path="/exception"/>
	[DebuggerStepThrough]
	public static FlagEnumerator<TEnum, ushort> EnumerateUInt16Flags<TEnum>(this TEnum value) where TEnum : unmanaged, Enum
	{
		return EnumerateFlags<TEnum, ushort>(value);
	}
}
