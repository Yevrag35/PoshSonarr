using System.Collections.Frozen;
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
/// Provides the frozen dictionary of metadata tags, keyed by name without regard to case, that a connection registers.
/// </summary>
/// <remarks>
/// <para>A <see cref="Builder"/> gathers the tags, and its <see cref="Builder.Freeze"/> method creates the resolver,
/// which can't change afterward. <see cref="MetadataResolverDI.AddMetadata"/> creates one for each cmdlet assembly and
/// registers that same instance for every connection, so every runspace in the process can read it at once without a
/// lock.</para>
/// <para>The tags sit in a <see cref="FrozenDictionary{TKey, TValue}"/>, which costs more to create than a
/// <see cref="Dictionary{TKey, TValue}"/> and less to read. The resolver is created once, while the indexer runs for
/// each object that the module reads from a response.</para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
internal sealed class MetadataResolver : IMetadataResolver
{
	/// <summary>
	/// The name of the note property that holds an object's tag.
	/// </summary>
	internal const string MetaPropertyName = "MetadataTag";
	/// <summary>
	/// The character that starts every tag's name.
	/// </summary>
	internal const char MetaPrefix = '#';
	/// <summary>
	/// The tags, keyed by name without regard to case.
	/// </summary>
	private readonly FrozenDictionary<string, MetadataTag> _dict;

	/// <summary>
	/// Gets the tag with the specified name.
	/// </summary>
	/// <param name="key">The tag's name, compared without regard to case.</param>
	/// <returns>The tag with the name, or <see cref="MetadataTag.Empty"/> when there's none or
	/// <paramref name="key"/> is null.</returns>
	public MetadataTag this[string key] => key is not null && _dict.TryGetValue(key, out MetadataTag? tag)
		? tag
		: MetadataTag.Empty;

	/// <summary>
	/// Gets the number of registered tags.
	/// </summary>
	/// <value>The number of tags.</value>
	public int Count => _dict.Count;

	/// <summary>
	/// Initializes a new <see cref="MetadataResolver"/> instance that looks tags up in the specified frozen dictionary.
	/// </summary>
	/// <param name="dict">The tags, keyed by name without regard to case.</param>
	private MetadataResolver(FrozenDictionary<string, MetadataTag> dict)
	{
		_dict = dict;
	}

	/// <inheritdoc/>
	public bool ContainsKey([NotNullWhen(true)] string? key)
	{
		return key is not null && _dict.ContainsKey(key);
	}
	/// <summary>
	/// Returns an enumerator over the registered tags.
	/// </summary>
	/// <returns>An enumerator over the tags, in no particular order.</returns>
	public IEnumerator<MetadataTag> GetEnumerator()
	{
		return ((IEnumerable<MetadataTag>)_dict.Values).GetEnumerator();
	}

	/// <summary>
	/// Returns an enumerator over the registered tags.
	/// </summary>
	/// <returns>An enumerator over the tags, in no particular order.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	/// <summary>
	/// Provides the mutable collection that gathers the metadata tags until <see cref="Freeze"/> creates a
	/// <see cref="MetadataResolver"/> from them.
	/// </summary>
	/// <remarks>
	/// The builder is enumerable and has an <see cref="Add(string, string, bool)"/> method, so a collection initializer
	/// can register its tags. It isn't thread-safe.
	/// </remarks>
	[DebuggerDisplay("Count = {Count}")]
	internal sealed class Builder : IEnumerable<MetadataTag>
	{
		/// <summary>
		/// The tags registered so far, keyed by name without regard to case.
		/// </summary>
		private readonly Dictionary<string, MetadataTag> _dict;
		/// <summary>
		/// The names of the cmdlets that each tag's objects can be piped to, keyed by tag name.
		/// </summary>
		private readonly Dictionary<string, ImmutableArray<string>> _pipesTo;

		/// <summary>
		/// Gets the number of tags registered so far.
		/// </summary>
		/// <value>The number of tags.</value>
		public int Count => _dict.Count;

		/// <summary>
		/// Initializes a new <see cref="Builder"/> instance with the specified capacity and cmdlet names.
		/// </summary>
		/// <param name="capacity">The number of tags that the builder holds before it grows.</param>
		/// <param name="pipesTo">The names of the cmdlets that each tag's objects can be piped to, keyed by tag name.</param>
		public Builder(int capacity, Dictionary<string, ImmutableArray<string>> pipesTo)
		{
			_dict = new(capacity, StringComparer.OrdinalIgnoreCase);
			_pipesTo = pipesTo;
		}

		/// <summary>
		/// Registers a tag for an endpoint.
		/// </summary>
		/// <remarks>
		/// Each name is registered once, compared without regard to case. A collection initializer discards what
		/// <c>Add</c> returns, so a second tag with a name that's registered already fails an assertion in a Debug
		/// build, and a Release build keeps the first tag.
		/// </remarks>
		/// <param name="tag">The tag's name.</param>
		/// <param name="baseUrl">The endpoint's path.</param>
		/// <param name="supportsId">Whether the endpoint takes an ID in its path.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> or <paramref name="baseUrl"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> or <paramref name="baseUrl"/> is empty.</exception>
		internal void Add(string tag, string baseUrl, bool supportsId)
		{
			ArgumentException.ThrowIfNullOrEmpty(tag);
			ArgumentException.ThrowIfNullOrEmpty(baseUrl);

			MetadataTag metadataTag = new(baseUrl, tag, supportsId, this.GetPipesTo(tag));

			bool added = _dict.TryAdd(tag, metadataTag);
			Debug.Assert(added, $"The metadata tag '{tag}' is registered more than once.");
		}
		/// <summary>
		/// Creates a resolver that holds the tags registered so far.
		/// </summary>
		/// <remarks>
		/// The resolver copies the tags into a frozen dictionary, so a tag that the builder registers afterward doesn't
		/// reach it, and each call creates a new resolver.
		/// </remarks>
		/// <returns>A <see cref="MetadataResolver"/> that holds the builder's tags.</returns>
		internal MetadataResolver Freeze()
		{
			// The comparer must be passed: ToFrozenDictionary uses the default comparer otherwise, not the source's.
			// Passing the source's own comparer lets it read _dict as it is, without copying it into a new Dictionary
			// first.
			return new(_dict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));
		}
		/// <summary>
		/// Returns an enumerator over the tags registered so far.
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
		/// Returns an enumerator over the tags registered so far.
		/// </summary>
		/// <returns>An enumerator over the tags, in no particular order.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}
