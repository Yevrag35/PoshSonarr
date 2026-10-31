using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Releases;

/// <summary>
/// Represents a release that a search found, from the <c>/release</c> endpoint.
/// </summary>
/// <remarks>
/// <para>
/// Once the object is deserialized, its <c>Age</c> property shows how old the release is as a <see cref="TimeSpan"/>
/// built from Sonarr's <c>ageMinutes</c>, its <c>AgeHours</c> and <c>AgeMinutes</c> properties are hidden, and a
/// <c>TotalWeight</c> property holds the sum of the four weights that <see cref="Weight"/> reads.
/// <see cref="OnSerializing"/> puts back what Sonarr sent, and <see cref="Reset"/> hides it again.
/// </para>
/// <para>
/// Releases sort by total weight, then by age, indexer ID, and release URL. The value of the <c>Protocol</c>
/// property starts with an uppercase letter.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class ReleaseObject : SonarrObject,
	IComparable<ReleaseObject>,
	IJsonOnSerializing,
	ISerializableNames<ReleaseObject>
{
	/// <summary>
	/// The number of properties that a new instance makes room for.
	/// </summary>
	const int CAPACITY = 46;
	/// <summary>
	/// The type's full name, which <see cref="SetPSTypeName"/> puts first in the PowerShell type names.
	/// </summary>
	static readonly string _typeName = typeof(ReleaseObject).GetName();

	/// <summary>
	/// The release's age in days, as Sonarr sent it.
	/// </summary>
	int _age;
	/// <summary>
	/// The release's weights, read when the object is deserialized.
	/// </summary>
	Weight _weight;
	/// <summary>
	/// The release's age in hours, as Sonarr sent it.
	/// </summary>
	double _ageHours;
	/// <summary>
	/// The release's age in minutes, as Sonarr sent it.
	/// </summary>
	double _ageMinutes;

	/// <summary>
	/// Gets how old the release is.
	/// </summary>
	/// <value>
	/// The age, built from Sonarr's <c>ageMinutes</c> when the object is deserialized, or <see cref="TimeSpan.Zero"/>
	/// before then.
	/// </value>
	public TimeSpan Age { get; private set; }
	/// <summary>
	/// Gets the ID of the indexer that found the release.
	/// </summary>
	/// <value>A copy of the <c>IndexerId</c> property, taken when the object is deserialized, or 0 before then.</value>
	public int IndexerId { get; private set; }
	/// <summary>
	/// Gets the release's identifier, from Sonarr's <c>guid</c> field, which is usually its URL.
	/// </summary>
	/// <remarks>
	/// The identifier is what a request that grabs the release sends as its <c>guid</c>.
	/// </remarks>
	/// <value>
	/// A copy of the <c>Guid</c> property, taken when the object is deserialized, or an empty string when Sonarr
	/// sent none.
	/// </value>
	public string ReleaseUrl { get; private set; } = string.Empty;
	/// <summary>
	/// Gets the sum of the release's weights.
	/// </summary>
	/// <value>
	/// The sum of the release's <c>LanguageWeight</c>, <c>PreferredWordScore</c>, <c>QualityWeight</c>, and
	/// <c>ReleaseWeight</c>, or 0 before the object is deserialized.
	/// </value>
	public int TotalWeight => _weight.TotalWeight;

	/// <summary>
	/// Initializes a new <see cref="ReleaseObject"/> instance with no properties, which the JSON converter fills.
	/// </summary>
	public ReleaseObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares this release with another by total weight, then by age, indexer ID, and release URL.
	/// </summary>
	/// <remarks>
	/// The release URLs compare without regard to case.
	/// </remarks>
	/// <param name="other">The release to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this release sorts before <paramref name="other"/>, 0 if they sort together, or a
	/// positive number if it sorts after <paramref name="other"/> or <paramref name="other"/> is
	/// <see langword="null"/>.
	/// </returns>
	public int CompareTo(ReleaseObject? other)
	{
		int compare = Comparer<int?>.Default.Compare(this.TotalWeight, other?.TotalWeight);
		if (compare != 0)
		{
			return compare;
		}

		compare = Comparer<TimeSpan?>.Default.Compare(this.Age, other?.Age);
		if (compare == 0)
		{
			compare = Comparer<int?>.Default.Compare(this.IndexerId, other?.IndexerId);
			if (compare == 0)
			{
				compare = StringComparer.InvariantCultureIgnoreCase.Compare(this.ReleaseUrl, other?.ReleaseUrl);
			}
		}

		return compare;
	}

	/// <summary>
	/// Gets the release tag from a resolver.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which this method ignores.</param>
	/// <returns>The resolver's <see cref="Meta.RELEASE"/> tag.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.RELEASE];
	}

	/// <summary>
	/// Finishes the object after the JSON converter reads it.
	/// </summary>
	/// <remarks>
	/// The base class's steps run first. Then the method copies the indexer ID, the release URL, and the three age
	/// fields, reads the weights, adds the <c>TotalWeight</c> property, builds <see cref="Age"/>, and calls
	/// <see cref="Reset"/>, which hides <c>AgeHours</c> and <c>AgeMinutes</c> and shows <c>Age</c> as a
	/// <see cref="TimeSpan"/>.
	/// </remarks>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		if (this.TryGetProperty(nameof(this.IndexerId), out int indexerId))
		{
			this.IndexerId = indexerId;
		}

		if (this.TryGetProperty("AgeMinutes", out double value))
		{
			_ageMinutes = value;
		}

		if (this.TryGetProperty("AgeHours", out double hours))
		{
			_ageHours = hours;
		}

		if (this.TryGetProperty(nameof(this.Age), out int days))
		{
			_age = days;
		}

		if (this.TryGetNonNullProperty("Guid", out string? releaseUrl))
		{
			this.ReleaseUrl = releaseUrl;
		}

		_weight = new(this);
		this.AddNumberProperty(nameof(this.TotalWeight), this.TotalWeight);

		this.Age = TimeSpan.FromMinutes(_ageMinutes);
		this.Reset();
	}

	/// <summary>
	/// Puts back the values that Sonarr sent before the release is written to a request body.
	/// </summary>
	/// <remarks>
	/// The method writes the four weights and the <c>IndexerId</c> and <c>ReleaseUrl</c> properties, makes
	/// <c>Age</c> the day count again, and adds <c>AgeHours</c> and <c>AgeMinutes</c> back.
	/// </remarks>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "<Pending>")]
	public void OnSerializing()
	{
		_weight.SetRelease(this);
		this.ReplaceNumberProperty(nameof(this.Age), _age);
		this.UpdateProperty(this.IndexerId, propertyName: nameof(IndexerId));
		this.UpdateProperty(x => x.ReleaseUrl);
		this.AddNumberProperty("AgeHours", _ageHours);
		this.AddNumberProperty("AgeMinutes", _ageMinutes);
	}

	/// <summary>
	/// Hides the release's <c>AgeHours</c> and <c>AgeMinutes</c> properties and shows its <c>Age</c> as a
	/// <see cref="TimeSpan"/>.
	/// </summary>
	public override void Reset()
	{
		this.Properties.RemoveMany("AgeHours", "AgeMinutes");
		this.ReplaceStructProperty(nameof(this.Age), this.Age);
		base.Reset();
	}

	/// <summary>
	/// Puts the type's full name first in the object's PowerShell type names, ahead of the name that the base class
	/// adds.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// The names of the string properties whose values get an uppercase first letter.
	/// </summary>
	static readonly HashSet<string> _capitalProps =
	[
		"Protocol",
	];
	/// <summary>
	/// Gets the names of the string properties whose values get an uppercase first letter.
	/// </summary>
	/// <returns>A set that holds <c>Protocol</c>.</returns>
	public static IReadOnlySet<string> GetPropertiesToCapitalize()
	{
		return _capitalProps;
	}
}
