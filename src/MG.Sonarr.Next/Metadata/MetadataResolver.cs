using System.Collections.Immutable;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// Represents the read-only set of metadata tags that a connection registers, one for each Sonarr API endpoint,
/// looked up by name.
/// </summary>
public interface IMetadataResolver : IReadOnlyCollection<MetadataTag>
{
	/// <summary>
	/// Gets the tag with the specified name.
	/// </summary>
	/// <param name="key">The tag's name, such as <c>#series</c>, compared without regard to case.</param>
	/// <returns>The tag with the name, or <see cref="MetadataTag.Empty"/> when there's none.</returns>
	MetadataTag this[string key] { get; }

	/// <summary>
	/// Determines whether a tag with the specified name is registered.
	/// </summary>
	/// <param name="key">The tag's name, compared without regard to case.</param>
	/// <returns>
	/// <see langword="true"/> if a tag named <paramref name="key"/> is registered; otherwise, <see langword="false"/>.
	/// </returns>
	bool ContainsKey([NotNullWhen(true)] string? key);
}

/// <summary>
/// Provides the dictionary of metadata tags, keyed by name without regard to case, that a connection registers.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
internal sealed class MetadataResolver : IMetadataResolver
{
	/// <summary>
	/// The name of the note property that holds an object's tag.
	/// </summary>
	public static readonly string META_PROPERTY_NAME = "MetadataTag";
	/// <summary>
	/// The character that starts every tag's name.
	/// </summary>
	public const char META_PREFIX = '#';
	/// <summary>
	/// The tags, keyed by name without regard to case.
	/// </summary>
	readonly Dictionary<string, MetadataTag> _dict;
	/// <summary>
	/// The names of the cmdlets that each tag's objects can be piped to, keyed by tag name.
	/// </summary>
	readonly Dictionary<string, ImmutableArray<string>> _pipesTo;

	/// <summary>
	/// Gets the tag with the specified name.
	/// </summary>
	/// <param name="key">The tag's name, compared without regard to case.</param>
	/// <returns>The tag with the name, or <see cref="MetadataTag.Empty"/> when there's none or
	/// <paramref name="key"/> is null.</returns>
	public MetadataTag this[string key] => _dict.TryGetValue(key ?? string.Empty, out MetadataTag? tag)
		? tag
		: MetadataTag.Empty;

	/// <summary>
	/// Gets the number of registered tags.
	/// </summary>
	/// <value>The number of tags.</value>
	public int Count => _dict.Count;

	/// <summary>
	/// Initializes a new <see cref="MetadataResolver"/> instance with the specified capacity and cmdlet names.
	/// </summary>
	/// <param name="capacity">The number of tags that the resolver holds before it grows.</param>
	/// <param name="pipesTo">The names of the cmdlets that each tag's objects can be piped to, keyed by tag name.</param>
	public MetadataResolver(int capacity, Dictionary<string, ImmutableArray<string>> pipesTo)
	{
		_dict = new(capacity, StringComparer.OrdinalIgnoreCase);
		_pipesTo = pipesTo;
	}

	/// <summary>
	/// Registers a tag for an endpoint, unless a tag with the same name is registered already.
	/// </summary>
	/// <param name="tag">The tag's name.</param>
	/// <param name="baseUrl">The endpoint's path.</param>
	/// <param name="supportsId">Whether the endpoint takes an ID in its path.</param>
	/// <returns>
	/// <see langword="true"/> if the tag was registered; <see langword="false"/> if one with the same name already was.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> or <paramref name="baseUrl"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> or <paramref name="baseUrl"/> is empty.</exception>
	internal bool Add(string tag, string baseUrl, bool supportsId)
	{
		ArgumentException.ThrowIfNullOrEmpty(tag);
		ArgumentException.ThrowIfNullOrEmpty(baseUrl);

		MetadataTag metadataTag = new(baseUrl, tag, supportsId, this.GetPipesTo(tag));

		return _dict.TryAdd(tag, metadataTag);
	}
	/// <inheritdoc/>
	public bool ContainsKey([NotNullWhen(true)] string? key)
	{
		return !string.IsNullOrWhiteSpace(key) && _dict.ContainsKey(key);
	}
	/// <summary>
	/// Returns an enumerator over the registered tags.
	/// </summary>
	/// <returns>An enumerator over the tags, in no particular order.</returns>
	public IEnumerator<MetadataTag> GetEnumerator()
	{
		return _dict.Values.GetEnumerator();
	}
	/// <summary>
	/// Gets the names of the cmdlets that a tag's objects can be piped to.
	/// </summary>
	/// <param name="key">The tag's name.</param>
	/// <returns>The cmdlet names, or an empty array when no cmdlet takes the tag.</returns>
	private ImmutableArray<string> GetPipesTo(string key)
	{
		return _pipesTo.TryGetValue(key, out ImmutableArray<string> pipesTo)
			? pipesTo
			: [];
	}

	/// <summary>
	/// Returns an enumerator over the registered tags.
	/// </summary>
	/// <returns>An enumerator over the tags, in no particular order.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}
}
