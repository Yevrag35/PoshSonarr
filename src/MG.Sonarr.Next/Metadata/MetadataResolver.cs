using System.Collections;
using System.Collections.Immutable;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// A dictionary interface of <see cref="MetadataTag"/> instances that describe the various
/// types of deserialized API response objects.
/// </summary>
public interface IMetadataResolver : IReadOnlyCollection<MetadataTag>
{
	MetadataTag this[string key] { get; }

	bool ContainsKey([NotNullWhen(true)] string? key);
}

/// <summary>
/// A dictionary implementation of <see cref="MetadataTag"/> instances that describe the various
/// types of deserialized API response objects.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
internal sealed class MetadataResolver : IMetadataResolver
{
	public static readonly string META_PROPERTY_NAME = "MetadataTag";
	public const char META_PREFIX = '#';
	readonly Dictionary<string, MetadataTag> _dict;
	readonly Dictionary<string, ImmutableArray<string>> _pipesTo;

	/// <summary>
	/// Gets the <see cref="MetadataTag"/> associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the <see cref="MetadataTag"/> to get; equal to 
	/// <see cref="MetadataTag.Value"/></param>
	/// <returns>
	///     The <see cref="MetadataTag"/> associated with the specified key. If the specified key is not
	///     found, <see cref="MetadataTag.Empty"/> is returned.
	/// </returns>
	public MetadataTag this[string key] => _dict.TryGetValue(key ?? string.Empty, out MetadataTag? tag)
		? tag
		: MetadataTag.Empty;

	public int Count => _dict.Count;

	public MetadataResolver(int capacity, Dictionary<string, ImmutableArray<string>> pipesTo)
	{
		_dict = new(capacity, StringComparer.OrdinalIgnoreCase);
		_pipesTo = pipesTo;
	}

	internal bool Add(string tag, string baseUrl, bool supportsId)
	{
		ArgumentException.ThrowIfNullOrEmpty(tag);
		ArgumentException.ThrowIfNullOrEmpty(baseUrl);

		MetadataTag metadataTag = new(baseUrl, tag, supportsId, this.GetPipesTo(tag));

		return _dict.TryAdd(tag, metadataTag);
	}
	public bool ContainsKey([NotNullWhen(true)] string? key)
	{
		return !string.IsNullOrWhiteSpace(key) && _dict.ContainsKey(key);
	}
	public IEnumerator<MetadataTag> GetEnumerator()
	{
		return _dict.Values.GetEnumerator();
	}
	private ImmutableArray<string> GetPipesTo(string key)
	{
		return _pipesTo.TryGetValue(key, out ImmutableArray<string> pipesTo)
			? pipesTo
			: [];
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}
}
