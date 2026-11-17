using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Models;

namespace MG.Sonarr.Next.Json;

/// <summary>
/// Defines static name mappings used during Sonarr object serialization and deserialization.
/// </summary>
/// <typeparam name="T">The Sonarr object type.</typeparam>
public interface ISerializableNames<T> where T : SonarrObject
{
	/// <summary>
	/// Gets property name mappings used while deserializing JSON.
	/// </summary>
	/// <returns>A read-only dictionary of source JSON names to model property names.</returns>
	static virtual IReadOnlyDictionary<string, string> GetDeserializedNames()
	{
		return EmptyNameDictionary.Empty<string>();
	}

	/// <summary>
	/// Gets property names that should be capitalized during naming transformations.
	/// </summary>
	/// <returns>A read-only set of property names to capitalize.</returns>
	static virtual IReadOnlySet<string> GetPropertiesToCapitalize()
	{
		return EmptyNameDictionary.Empty<string>();
	}

	/// <summary>
	/// Gets property name mappings used while serializing JSON.
	/// </summary>
	/// <returns>A read-only dictionary of model property names to JSON names.</returns>
	static virtual IReadOnlyDictionary<string, string> GetSerializedNames()
	{
		return EmptyNameDictionary.Empty<string>();
	}
}
