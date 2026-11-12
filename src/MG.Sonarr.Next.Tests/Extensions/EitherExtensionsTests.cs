using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Exceptions.Shell;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Tests.Extensions;

/// <summary>
/// Tests the split of a cmdlet's names and IDs into the sets that it looks them up by.
/// </summary>
public sealed class EitherExtensionsTests
{
	/// <summary>
	/// <see cref="EitherExtensions.SplitToSets"/> refuses an array that holds a default element, which holds neither a
	/// name nor an ID, as PowerShell binds a <c>$null</c> element of a list and <c>@{}</c>, before it adds anything to
	/// either set, whether it reads numbers as IDs or as names. So a cmdlet never looks up an ID of 0 for such an
	/// element, an ID that its validation refuses when a user types it, and never gets everything for a list that names
	/// nothing.
	/// </summary>
	/// <param name="explicitlyCalledForString">Whether every value is read as a name, as for <c>-Name</c> given by
	/// name.</param>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RefusesADefaultElementBeforeAddingAnything(bool explicitlyCalledForString)
	{
		Either<string, int>[] names = ["abc", 5, default];
		HashSet<int> numbers = [];
		WildcardSet strings = [];

		var e = Assert.Throws<SonarrParameterException>(() => names.SplitToSets(numbers, strings, explicitlyCalledForString));

		Assert.Equal(nameof(names), e.ParameterName);
		Assert.Equal(ParameterErrorType.Invalid, e.ErrorType);
		Assert.Empty(numbers);
		Assert.Empty(strings);
	}
}
