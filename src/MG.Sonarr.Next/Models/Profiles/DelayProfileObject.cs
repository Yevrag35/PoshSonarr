using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles;

/// <summary>
/// Represents a Sonarr delay profile, from the <c>/delayprofile</c> endpoint.
/// </summary>
/// <remarks>
/// <para>
/// Sonarr's <c>torrentDelay</c> and <c>usenetDelay</c> fields become the <c>TorrentDelayInMins</c> and
/// <c>UsenetDelayInMins</c> properties, and a request body writes them back under Sonarr's names.
/// </para>
/// <para>
/// The value of the <c>PreferredProtocol</c> property starts with an uppercase letter.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class DelayProfileObject : TagUpdateObject<DelayProfileObject>,
	ISerializableNames<DelayProfileObject>
{
	/// <summary>
	/// The number of properties that a new instance makes room for.
	/// </summary>
	const int CAPACITY = 10;
	/// <summary>
	/// The type's full name, which <see cref="SetPSTypeName"/> puts first in the PowerShell type names.
	/// </summary>
	static readonly string _typeName = typeof(DelayProfileObject).GetName();

	/// <summary>
	/// Initializes a new <see cref="DelayProfileObject"/> instance with no properties, which the JSON converter fills.
	/// </summary>
	public DelayProfileObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the delay profile tag from a resolver.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which this method ignores.</param>
	/// <returns>The resolver's <see cref="Meta.DELAY_PROFILE"/> tag.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.DELAY_PROFILE];
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
	static readonly OneStringSet _capitalSet = new("PreferredProtocol");
	/// <summary>
	/// The JSON name maps, built the first time they're read.
	/// </summary>
	static readonly Lazy<JsonNameHolder> _names = new(GetJsonNames);
	/// <summary>
	/// Builds the JSON name maps from the pairs that rename the two delay fields.
	/// </summary>
	/// <returns>A <see cref="JsonNameHolder"/> that holds the maps in both directions.</returns>
	private static JsonNameHolder GetJsonNames()
	{
		return JsonNameHolder
			.FromDeserializationNamePairs(new KeyValuePair<string, string>[]
			{
				new("TorrentDelay", "TorrentDelayInMins"),
				new("UsenetDelay", "UsenetDelayInMins"),
			});
	}

	/// <summary>
	/// Gets the map from a JSON field's name, with its first letter uppercased, to the name of the property that
	/// holds its value.
	/// </summary>
	/// <returns>
	/// A map that renames <c>TorrentDelay</c> to <c>TorrentDelayInMins</c> and <c>UsenetDelay</c> to
	/// <c>UsenetDelayInMins</c>.
	/// </returns>
	public static IReadOnlyDictionary<string, string> GetDeserializedNames()
	{
		return _names.Value.DeserializationNames;
	}
	/// <summary>
	/// Gets the names of the string properties whose values get an uppercase first letter.
	/// </summary>
	/// <returns>A set that holds <c>PreferredProtocol</c>.</returns>
	public static IReadOnlySet<string> GetPropertiesToCapitalize()
	{
		return _capitalSet;
	}
	/// <summary>
	/// Gets the map from a property's name to the name of the JSON field that a request body writes its value under.
	/// </summary>
	/// <returns>
	/// A map that renames <c>TorrentDelayInMins</c> to <c>TorrentDelay</c> and <c>UsenetDelayInMins</c> to
	/// <c>UsenetDelay</c>.
	/// </returns>
	public static IReadOnlyDictionary<string, string> GetSerializedNames()
	{
		return _names.Value.SerializationNames;
	}
}
