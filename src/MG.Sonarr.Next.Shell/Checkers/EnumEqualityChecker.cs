using System.Numerics;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Shell.Checkers;

/// <summary>
/// Represents an equality checker for an enum type, which converts both values to the enum and compares their numbers.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MG.Sonarr.Next.Shell.Attributes.DistinctValuesAttribute"/> picks this checker for an enum element type,
/// such as <see cref="DayOfWeek"/>. Values are converted as PowerShell converts them, so a member's name and its number
/// both equal the member, and a value that doesn't convert is equal to nothing but itself. The checker holds no state
/// that changes after it's created, so it's thread-safe.
/// </para>
/// <para><b>Performance:</b> When the enum's underlying type is <see cref="int"/>, <see cref="long"/>, or
/// <see cref="byte"/>, the checker reads each value as that number in place, without boxing. An enum with any other
/// underlying type is compared through <see cref="Enum.Equals(object)"/>, which boxes.</para>
/// </remarks>
/// <typeparam name="T">
/// The enum type. Because it's unmanaged, a value can be read as a number of its underlying type.
/// </typeparam>
public sealed class EnumEqualityChecker<T> : EqualityChecker where T : unmanaged, Enum
{
	private readonly Type _type;
	private readonly Type _underlyingType;

	/// <summary>
	/// Initializes a new <see cref="EnumEqualityChecker{T}"/> instance, which looks up the underlying type of
	/// <typeparamref name="T"/>.
	/// </summary>
	public EnumEqualityChecker()
	{
		_type = typeof(T);
		_underlyingType = Enum.GetUnderlyingType(_type);
	}

	/// <summary>
	/// Converts both objects to the enum type and determines whether the values are equal.
	/// </summary>
	/// <param name="x">The first object, which isn't <see langword="null"/>.</param>
	/// <param name="y">The second object, which isn't <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if both objects convert to <typeparamref name="T"/> and the values have the same number;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	protected override bool EqualsCore([DisallowNull] object x, [DisallowNull] object y)
	{
		if (!LanguagePrimitives.TryConvertTo(x, out T xValue)
			||
			!LanguagePrimitives.TryConvertTo(y, out T yValue))
		{
			return false;
		}

		return this.AreValuesEqual(ref xValue, ref yValue);
	}

	/// <summary>
	/// Determines whether two enum values have the same number, read as the enum's underlying type.
	/// </summary>
	/// <param name="x">The first value.</param>
	/// <param name="y">The second value.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> have the same number; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private bool AreValuesEqual(ref T x, ref T y)
	{
		unsafe
		{
			// SAFETY: Each branch passes the underlying type that it just found _underlyingType to be.
			if (_underlyingType.Equals(typeof(int)))
			{
				return AreValuesEqual<int>(ref x, ref y);
			}
			else if (_underlyingType.Equals(typeof(long)))
			{
				return AreValuesEqual<long>(ref x, ref y);
			}
			else if (_underlyingType.Equals(typeof(byte)))
			{
				return AreValuesEqual<byte>(ref x, ref y);
			}
			else
			{
				return x.Equals(y);
			}
		}
	}

	/// <summary>
	/// Determines whether two enum values are equal, read in place as numbers of the specified type.
	/// </summary>
	/// <safety>
	/// <typeparamref name="TNum"/> must be the underlying type of <typeparamref name="T"/>, since the values are
	/// reinterpreted as it without a check.
	/// </safety>
	/// <typeparam name="TNum">The enum's underlying type.</typeparam>
	/// <param name="x">The first value.</param>
	/// <param name="y">The second value.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> are the same number; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static unsafe bool AreValuesEqual<TNum>(ref T x, ref T y) where TNum : unmanaged, INumber<TNum>
	{
		unsafe
		{
			// SAFETY: The caller passes the underlying type of T as TNum, as the safety contract requires, so each
			// reinterpretation reads exactly the bytes of its value.
			ref readonly TNum xAsNum = ref Unsafe.As<T, TNum>(ref x);
			ref readonly TNum yAsNum = ref Unsafe.As<T, TNum>(ref y);

			return xAsNum.Equals(yAsNum);
		}
	}

	/// <summary>
	/// Gets the object's own hash code.
	/// </summary>
	/// <remarks>
	/// The attribute converts each value to the enum type before it compares them, so the object is a boxed
	/// <typeparamref name="T"/>, and its hash code is that of its number.
	/// </remarks>
	/// <param name="obj">The object, which isn't <see langword="null"/>.</param>
	/// <returns>The value that <paramref name="obj"/>'s <see cref="object.GetHashCode"/> returns.</returns>
	protected override int GetHashCodeCore([DisallowNull] object obj)
	{
		return obj.GetHashCode();
	}
}
