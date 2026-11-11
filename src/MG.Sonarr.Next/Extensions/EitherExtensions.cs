using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Either{TLeft, TRight}"/> types.
/// </summary>
public static class EitherExtensions
{
	/// <summary>
	/// Splits an array of <see cref="Either{TLeft, TRight}"/> into a set of IDs and a set of name patterns.
	/// </summary>
	/// <remarks>
	/// When <paramref name="explicitlyCalledForString"/> is <see langword="false"/>, as for a positional argument that
	/// can be either an ID or a name, a number goes to <paramref name="numbers"/>, and so does text that reads as a whole
	/// number in the invariant culture, such as <c>'5'</c> or <c>'+5'</c>, since a user's input is read the same way in
	/// every culture. Any other text goes to <paramref name="strings"/>. When it's <see langword="true"/>, as for a
	/// <c>-Name</c> given by name, every value goes to <paramref name="strings"/>, a number as its invariant text.
	/// </remarks>
	/// <param name="array">The array of <see cref="Either{TLeft, TRight}"/> to be split.</param>
	/// <param name="numbers">The set to which the IDs are added.</param>
	/// <param name="strings">The set to which the name patterns are added.</param>
	/// <param name="explicitlyCalledForString">
	/// <see langword="true"/> to read every value as a name; otherwise, <see langword="false"/>.
	/// </param>
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
				1 when !explicitlyCalledForString && int.TryParse(item.AsT1, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) => numbers.Add(number),
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
	[SkipLocalsInit]
	private static bool AddSpan(WildcardSet set, int value)
	{
		unsafe
		{
			// SAFETY: [SkipLocalsInit] leaves chars uninitialized, but only its first written characters are read, and
			// int.TryFormat wrote those and reports no more than chars holds.
			Span<char> chars = stackalloc char[LengthConstants.INT_MAX];
			if (value.TryFormat(chars, out int written, provider: CultureInfo.InvariantCulture))
			{
				set.Add(chars.UnsafeSlice(0, written));
				return true;
			}
		}

		return false;
	}
}