using MG.Sonarr.Next.Extensions.PSO;
using System.Collections.Immutable;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.Releases;

/// <summary>
/// Represents the four weights that Sonarr gives a release, and their sum.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct Weight
{
	/// <summary>
	/// The names of a release's weight properties: <c>LanguageWeight</c>, <c>PreferredWordScore</c>,
	/// <c>QualityWeight</c>, and <c>ReleaseWeight</c>, in that order.
	/// </summary>
	static readonly ImmutableArray<string> _weightProps =
	[
		"LanguageWeight", "PreferredWordScore", "QualityWeight", "ReleaseWeight"
	];

	/// <summary>
	/// The language weight.
	/// </summary>
	readonly int _langWeight;
	/// <summary>
	/// The release weight.
	/// </summary>
	readonly int _releaseWeight;
	/// <summary>
	/// The quality weight.
	/// </summary>
	readonly int _qualWeight;
	/// <summary>
	/// The preferred-word score.
	/// </summary>
	readonly int _prefWord;
	/// <summary>
	/// The sum of the four weights.
	/// </summary>
	readonly int _total;

	/// <summary>
	/// Gets the weight that the release's language gives it.
	/// </summary>
	/// <value>The value of the release's <c>LanguageWeight</c>, or 0 when it had none.</value>
	public int LanguageWeight => _langWeight;
	/// <summary>
	/// Gets the score that the preferred words of the release profiles give the release.
	/// </summary>
	/// <value>The value of the release's <c>PreferredWordScore</c>, or 0 when it had none.</value>
	public int PreferredWordScore => _prefWord;
	/// <summary>
	/// Gets the release's own weight.
	/// </summary>
	/// <value>The value of the release's <c>ReleaseWeight</c>, or 0 when it had none.</value>
	public int ReleaseWeight => _releaseWeight;
	/// <summary>
	/// Gets the weight that the release's quality gives it.
	/// </summary>
	/// <value>The value of the release's <c>QualityWeight</c>, or 0 when it had none.</value>
	public int QualityWeight => _qualWeight;
	/// <summary>
	/// Gets the sum of the four weights.
	/// </summary>
	/// <value>
	/// The sum of <see cref="LanguageWeight"/>, <see cref="PreferredWordScore"/>, <see cref="QualityWeight"/>, and
	/// <see cref="ReleaseWeight"/>.
	/// </value>
	public int TotalWeight => _total;

	/// <summary>
	/// Initializes a new <see cref="Weight"/> instance with the weights read from a release's properties.
	/// </summary>
	/// <remarks>
	/// A weight whose property is missing, or holds neither an <see cref="int"/> nor a string that parses as one,
	/// reads as 0.
	/// </remarks>
	/// <param name="release">The release to read the weights from.</param>
	public Weight(ReleaseObject release)
	{
		int langWeight = GetValueOrDefault(release, _weightProps[0]);
		int prefWord = GetValueOrDefault(release, _weightProps[1]);
		int qualWeight = GetValueOrDefault(release, _weightProps[2]);
		int releaseWeight = GetValueOrDefault(release, _weightProps[3]);
		_total = langWeight + prefWord + qualWeight + releaseWeight;
		_langWeight = langWeight;
		_qualWeight = qualWeight;
		_prefWord = prefWord;
		_releaseWeight = releaseWeight;
	}

	/// <summary>
	/// Reads one weight from a release's properties.
	/// </summary>
	/// <param name="release">The release to read the weight from.</param>
	/// <param name="propertyName">The name of the weight's property.</param>
	/// <returns>
	/// The property's <see cref="int"/> value, the number that its string value parses as, or 0 when it's missing or
	/// holds anything else.
	/// </returns>
	private static int GetValueOrDefault(ReleaseObject release, string propertyName)
	{
		PSPropertyInfo? prop = release.Properties[propertyName];
		return prop?.Value switch
		{
			int i => i,
			string s => int.TryParse(s, out int isNum) ? isNum : 0,
			_ => 0,
		};
	}

	/// <summary>
	/// Writes the four weights back to a release's properties.
	/// </summary>
	/// <param name="release">The release to write the weights to.</param>
	public void SetRelease(ReleaseObject release)
	{
		release.UpdateProperty(_langWeight, propertyName: _weightProps[0]);
		release.UpdateProperty(_prefWord, propertyName: _weightProps[1]);
		release.UpdateProperty(_qualWeight, propertyName: _weightProps[2]);
		release.UpdateProperty(_releaseWeight, propertyName: _weightProps[3]);
	}
}
