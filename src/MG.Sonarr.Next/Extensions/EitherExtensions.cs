using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Exceptions.Shell;
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
	/// <para>
	/// When <paramref name="explicitlyCalledForString"/> is <see langword="false"/>, as for a positional argument that
	/// can be either an ID or a name, a number goes to <paramref name="numbers"/>, and so does text that reads as a whole
	/// number in the invariant culture, such as <c>'5'</c> or <c>'+5'</c>, since a user's input is read the same way in
	/// every culture. Any other text goes to <paramref name="strings"/>. When it's <see langword="true"/>, as for a
	/// <c>-Name</c> given by name, every value goes to <paramref name="strings"/>, a number as its invariant text.
	/// </para>
	/// <para>
	/// A <see langword="default"/> element holds neither a name nor an ID, and PowerShell binds a <c>$null</c> element
	/// of a list, <c>@{}</c>, and <c>[pscustomobject]@{}</c> as one. The method refuses an array that holds one before
	/// it adds anything to either set, so a cmdlet neither looks up an ID of 0 for it nor, with no ID or name left,
	/// gets everything.
	/// </para>
	/// </remarks>
	/// <param name="array">The array of <see cref="Either{TLeft, TRight}"/> to be split.</param>
	/// <param name="numbers">The set to which the IDs are added.</param>
	/// <param name="strings">The set to which the name patterns are added.</param>
	/// <param name="explicitlyCalledForString">
	/// <see langword="true"/> to read every value as a name; otherwise, <see langword="false"/>.
	/// </param>
	/// <param name="paramName">
	/// The name of the cmdlet parameter that <paramref name="array"/> came from, for the exception's message. The
	/// compiler supplies the expression that the caller passes as <paramref name="array"/>, such as <c>this.Name</c>,
	/// and the method drops its leading <c>this.</c>.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="numbers"/> or <paramref name="strings"/> is
	/// null.</exception>
	/// <exception cref="SonarrParameterException">Thrown when <paramref name="array"/> holds a default element, which
	/// holds neither a name nor an ID.</exception>
	public static void SplitToSets(
		this Either<string, int>[]? array,
		ISet<int> numbers,
		WildcardSet strings,
		bool explicitlyCalledForString = false,
		[CallerArgumentExpression(nameof(array))] string paramName = "")
	{
		ArgumentNullException.ThrowIfNull(numbers);
		ArgumentNullException.ThrowIfNull(strings);

		if (array is null)
		{
			return;
		}

		foreach (Either<string, int> item in array)
		{
			if (item.IsDefaultOrEmpty)
			{
				ThrowHoldsNeither(paramName);
			}
		}

		foreach (Either<string, int> item in array)
		{
			// The loop above refused index 0, so the last arm takes only an ID.
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
	/// Throws the exception for an array that holds an element with neither a name nor an ID.
	/// </summary>
	/// <param name="paramName">The expression that the caller passed as the array, such as <c>this.Name</c>.</param>
	/// <exception cref="SonarrParameterException">Thrown when this method is called, naming the parameter without a
	/// leading <c>this.</c>.</exception>
	[DoesNotReturn]
	private static void ThrowHoldsNeither(string paramName)
	{
		const string THIS = "this.";
		string name = paramName.StartsWith(THIS, StringComparison.Ordinal)
			? paramName.Substring(THIS.Length)
			: paramName;

		throw new SonarrParameterException(name, ParameterErrorType.Invalid,
			"It holds an empty value, such as $null or @{}, that is neither a name nor an ID.");
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