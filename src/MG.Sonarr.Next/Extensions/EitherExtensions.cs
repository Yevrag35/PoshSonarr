using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Either{TLeft, TRight}"/> types.
/// </summary>
public static class EitherExtensions
{
	/// <summary>
	/// Splits an array of <see cref="Either{TLeft, TRight}"/> into two separate sets: one for the left values and one for the right values.
	/// </summary>
	/// <param name="array">The array of <see cref="Either{TLeft, TRight}"/> to be split.</param>
	/// <param name="numbers">The set to which the right values (integers) will be added.</param>
	/// <param name="strings">The set to which the left values (strings) will be added.</param>
	/// <param name="explicitlyCalledForString">A boolean flag indicating whether the method was explicitly called for string values.</param>
	public static void SplitToSets(this Either<string, int>[]? array, ISet<int> numbers, WildcardSet strings, bool explicitlyCalledForString = false)
	{
		ArgumentNullException.ThrowIfNull(numbers);
		ArgumentNullException.ThrowIfNull(strings);

		if (array is null)
		{
			return;
		}

		foreach (Either<string, int> item in array)
		{
			_ = item.Index switch
			{
				1 when !explicitlyCalledForString && int.TryParse(item.AsT1, out int number) => numbers.Add(number),
				1 => strings.Add(item.AsT1),
				2 when explicitlyCalledForString => AddSpan(strings, item.AsT2),
				_ => numbers.Add(item.AsT2),
			};
		}
	}

	/// <summary>
	/// Adds the string representation of the given integer value to the specified <see cref="WildcardSet"/>.
	/// </summary>
	/// <param name="set">The <see cref="WildcardSet"/> to which the string representation of the integer value will be added.</param>
	/// <param name="value">The integer value to be added to the <see cref="WildcardSet"/>.</param>
	/// <returns><see langword="true"/> if the value was successfully added; otherwise, <see langword="false"/>.</returns>
	private static bool AddSpan(WildcardSet set, int value)
	{
#if NET9_0_OR_GREATER
		Span<char> chars = stackalloc char[LengthConstants.INT_MAX];
		if (value.TryFormat(chars, out int written, provider: CultureInfo.InvariantCulture))
		{
			set.Add(chars.UnsafeSlice(0, written));
			return true;
		}

		return false;
#else
        return set.Add(value.ToString());
#endif
	}
}