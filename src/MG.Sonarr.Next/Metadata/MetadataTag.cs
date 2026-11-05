using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Services.Http.Queries;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// Represents the tag that identifies which Sonarr API endpoint an object came from and which cmdlets it can be piped
/// to.
/// </summary>
/// <remarks>
/// <para>Instances are immutable. Get them from an <see cref="IMetadataResolver"/>, which holds one for each
/// endpoint. The module creates them the first time it builds a connection, and every later connection shares
/// them.</para>
/// <para>The tag also builds the URLs of requests to its endpoint, from its <see cref="UrlBase"/>.</para>
/// </remarks>
[DebuggerDisplay(@"\{{Value}, {UrlBase}\}")]
public sealed class MetadataTag : ICloneable, IComparable<MetadataTag>, IEquatable<MetadataTag>
{
	/// <summary>
	/// The largest buffer, in characters, that <see cref="GetUrlForId{T}(T, QueryCol)"/> and <see cref="ToString"/>
	/// allocate on the stack instead of renting a pooled array.
	/// </summary>
	private const int MAX_STACKALLOC = 256;

	/// <summary>
	/// The cached <see cref="ToString"/> string.
	/// </summary>
	private string? _toString;

	/// <summary>
	/// Gets the names of the cmdlets that objects with this tag can be piped to.
	/// </summary>
	/// <value>The cmdlet names, such as <c>Get-SonarrSeries</c>, in ordinal order, or an empty array.</value>
	public ImmutableArray<string> CanPipeTo { get; }
	/// <summary>
	/// Gets a value that indicates whether the tag's endpoint takes an ID in its path.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the endpoint takes an ID, as <c>/series/{id}</c> does; otherwise,
	/// <see langword="false"/>.
	/// </value>
	public bool SupportsId { get; }
	/// <summary>
	/// Gets the path of the Sonarr API endpoint that this tag represents.
	/// </summary>
	/// <value>The endpoint's path, relative to the API's root and without a trailing slash, such as <c>/series</c>.</value>
	public string UrlBase { get; }
	/// <summary>
	/// Gets the tag's name, which PowerShell shows in an object's <c>MetadataTag</c> property.
	/// </summary>
	/// <value>The tag's name, such as <c>#series</c>.</value>
	public string Value { get; }

	/// <summary>
	/// Initializes a new <see cref="MetadataTag"/> instance with an empty name and path, for
	/// <see cref="Empty"/>.
	/// </summary>
	private MetadataTag()
		: this(string.Empty, string.Empty, supportsId: false, [])
	{
	}
	/// <summary>
	/// Initializes a new <see cref="MetadataTag"/> instance with the values of another tag.
	/// </summary>
	/// <param name="copyFrom">The tag to copy.</param>
	private MetadataTag(MetadataTag copyFrom)
		: this(copyFrom.UrlBase, copyFrom.Value, copyFrom.SupportsId, copyFrom.CanPipeTo)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="MetadataTag"/> instance with the specified endpoint path, name, ID support, and
	/// cmdlet names.
	/// </summary>
	/// <param name="urlBase">The endpoint's path. A trailing slash is removed.</param>
	/// <param name="value">The tag's name.</param>
	/// <param name="supportsId">Whether the endpoint takes an ID in its path.</param>
	/// <param name="pipesTo">The names of the cmdlets that objects with the tag can be piped to.</param>
	internal MetadataTag(string urlBase, string value, bool supportsId, ImmutableArray<string> pipesTo)
	{
		this.UrlBase = urlBase.TrimEnd('/');
		this.Value = value;
		this.SupportsId = supportsId;
		this.CanPipeTo = pipesTo;
	}

	/// <summary>
	/// Creates a copy of the tag.
	/// </summary>
	/// <returns>A new <see cref="MetadataTag"/> that equals this one.</returns>
	[DebuggerStepThrough]
	object ICloneable.Clone() => this.Clone();
	/// <summary>
	/// Creates a copy of the tag.
	/// </summary>
	/// <returns>A new <see cref="MetadataTag"/> that equals this one.</returns>
	public MetadataTag Clone() => new(this);

	/// <summary>
	/// The tag with no name or path, which no cmdlet takes. It marks an object that hasn't been tagged or that comes
	/// from no known endpoint.
	/// </summary>
	public static readonly MetadataTag Empty = new();

	/// <summary>
	/// Compares the tag with another by an ordinal comparison of their <see cref="Value"/>.
	/// </summary>
	/// <param name="other">The tag to compare with.</param>
	/// <returns>
	/// A negative number if this tag sorts before <paramref name="other"/>, 0 if they sort together, and a positive
	/// number if this tag sorts after it or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(MetadataTag? other)
	{
		return this.RefEqualsOrNull(other, out bool isEqual)
			? isEqual ? 0 : 1
			: string.Compare(this.Value, other.Value, StringComparison.Ordinal);
	}

	/// <summary>
	/// Determines whether the tag equals another.
	/// </summary>
	/// <remarks>
	/// Two tags are equal when their <see cref="SupportsId"/> values match and their <see cref="UrlBase"/> and
	/// <see cref="Value"/> match ordinally, case included. <see cref="CanPipeTo"/> isn't compared.
	/// </remarks>
	/// <param name="other">The tag to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="other"/> equals this tag; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Equals([NotNullWhen(true)] MetadataTag? other)
	{
		return other is not null && this.EqualsCore(other);
	}
	/// <summary>
	/// Determines whether the tag equals an object.
	/// </summary>
	/// <param name="obj">The object to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="obj"/> is a <see cref="MetadataTag"/> that equals this tag;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return obj is MetadataTag tag && this.EqualsCore(tag);
	}

	/// <summary>
	/// Determines whether the tag equals another, without checking for null or reference equality.
	/// </summary>
	/// <param name="other">The tag to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="other"/> equals this tag; otherwise, <see langword="false"/>.
	/// </returns>
	private bool EqualsCore(MetadataTag other)
	{
		return ReferenceEquals(this, other)
			   ||
			   (this.SupportsId == other.SupportsId
			   &&
			   this.Value.Equals(other.Value, StringComparison.Ordinal)
			   &&
			   this.UrlBase.Equals(other.UrlBase, StringComparison.Ordinal));
	}

	/// <summary>
	/// Gets a hash code for the tag.
	/// </summary>
	/// <returns>A hash code of <see cref="UrlBase"/>, <see cref="Value"/>, and <see cref="SupportsId"/>.</returns>
	public override int GetHashCode()
	{
		HashCode hc = new();
		hc.Add(this.SupportsId);
		hc.Add(this.UrlBase, StringComparer.Ordinal);
		hc.Add(this.Value, StringComparer.Ordinal);
		return hc.ToHashCode();
	}

	/// <summary>
	/// Builds the URL of a request to the tag's endpoint with the specified query string.
	/// </summary>
	/// <param name="parameters">The query parameters to append, or <see langword="null"/> for none.</param>
	/// <returns><see cref="UrlBase"/>, followed by the query string of <paramref name="parameters"/>, if any.</returns>
	public string GetUrl(QueryCol? parameters)
	{
		return parameters.GetUrl(this.UrlBase);
	}
	/// <summary>
	/// Builds the URL of a request to the tag's endpoint with a query string of one field.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The field is formatted with the invariant culture, so the URL reads the same whatever the current culture is. A
	/// field whose <see cref="IQueryField.MaxLength"/> is 0, such as a default one, adds nothing after the <c>?</c>.
	/// </para>
	/// <para>
	/// The URL is built in a stack buffer when it's 256 characters or fewer, and in a pooled buffer otherwise, so the only
	/// allocation is the returned string.
	/// </para>
	/// </remarks>
	/// <param name="parameter">The query field to append.</param>
	/// <returns><see cref="UrlBase"/>, followed by <c>?</c> and the formatted field.</returns>
	/// <exception cref="FormatException">Thrown when <paramref name="parameter"/> doesn't fit in its <see cref="IQueryField.MaxLength"/>.</exception>
	public string GetUrl(IQueryField parameter)
	{
		int maxLength = parameter.MaxLength;
		int length = this.UrlBase.Length + 1 + maxLength;

		using var builder = SpanStringBuilder.Create(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length);

		builder.Append(this.UrlBase);
		builder.Append('?');
		if (maxLength > 0)
		{
			builder.Append(parameter, maxLength, provider: CultureInfo.InvariantCulture);
		}

		return builder.ToString();
	}

	/// <summary>
	/// Builds the URL of a request for the item with the specified ID at the tag's endpoint.
	/// </summary>
	/// <param name="id">The item's ID, or a null, empty, or white-space string for the endpoint itself.</param>
	/// <returns><see cref="UrlBase"/>, followed by <c>/</c> and <paramref name="id"/> when it isn't blank.</returns>
	/// <exception cref="InvalidOperationException">Thrown when <see cref="SupportsId"/> is false.</exception>
	public string GetUrlForId(string? id)
	{
		this.ThrowIfNotSupportId();
		if (string.IsNullOrWhiteSpace(id))
		{
			return this.UrlBase;
		}

		return string.Concat(this.UrlBase, ['/'], id);
	}
	/// <summary>
	/// Builds the URL of a request for the item with the specified ID at the tag's endpoint.
	/// </summary>
	/// <param name="id">The item's ID, or an empty or white-space span for the endpoint itself.</param>
	/// <returns><see cref="UrlBase"/>, followed by <c>/</c> and <paramref name="id"/> when it isn't blank.</returns>
	/// <exception cref="InvalidOperationException">Thrown when <see cref="SupportsId"/> is false.</exception>
	public string GetUrlForId(ReadOnlySpan<char> id)
	{
		this.ThrowIfNotSupportId();
		if (id.IsWhiteSpace())
		{
			return this.UrlBase;
		}

		return string.Concat(this.UrlBase, ['/'], id);
	}

	/// <summary>
	/// Builds the URL of a request for the item with the specified ID at the tag's endpoint.
	/// </summary>
	/// <remarks>
	/// The ID is formatted with the invariant culture into a stack buffer of 40 characters, which holds any integer up
	/// to an <see cref="Int128"/>. An ID whose text is longer is formatted to a string of its own, which the URL is
	/// then concatenated from.
	/// </remarks>
	/// <typeparam name="T">The type of the ID, such as <see cref="int"/>.</typeparam>
	/// <param name="id">The item's ID.</param>
	/// <returns><see cref="UrlBase"/>, followed by <c>/</c> and the formatted <paramref name="id"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when <see cref="SupportsId"/> is false.</exception>
	public string GetUrlForId<T>(T id) where T : ISpanFormattable
	{
		this.ThrowIfNotSupportId();
		Span<char> span = stackalloc char[this.UrlBase.Length + 1 + LengthConstants.INT128_MAX];
		this.UrlBase.CopyTo(span, out int position);

		span[position++] = '/';

		return id.TryCopyToSlice(span, ref position, provider: CultureInfo.InvariantCulture)
			? new string(span.Slice(0, position))
			: string.Concat(this.UrlBase, "/", id.ToString(format: null, formatProvider: CultureInfo.InvariantCulture));
	}
	/// <summary>
	/// Builds the URL of a request for the item with the specified ID at the tag's endpoint, with a query string.
	/// </summary>
	/// <typeparam name="T">The type of the ID, such as <see cref="int"/>.</typeparam>
	/// <param name="id">The item's ID, which formats to at most 40 characters.</param>
	/// <param name="parameters">The query parameters to append.</param>
	/// <returns>
	/// <see cref="UrlBase"/>, followed by <c>/</c>, the formatted <paramref name="id"/>, and the query string of
	/// <paramref name="parameters"/>.
	/// </returns>
	/// <exception cref="InvalidOperationException">Thrown when <see cref="SupportsId"/> is false.</exception>
	public string GetUrlForId<T>(T id, QueryCol parameters) where T : ISpanFormattable
	{
		this.ThrowIfNotSupportId();
		int length = this.UrlBase.Length + 2 + parameters.MaxLength + LengthConstants.INT128_MAX;

		using var builder = SpanStringBuilder.Create(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length);

		unsafe { builder.Append(this.UrlBase.Length + 1, this.UrlBase, &writeUrlBase); }

		builder.Append(id, LengthConstants.INT128_MAX, provider: CultureInfo.InvariantCulture);
		builder.Append(parameters, parameters.MaxLength);

		return builder.ToString();

		static void writeUrlBase(Span<char> buffer, string urlBase)
		{
			urlBase.CopyTo(buffer.UnsafeSlice(0, urlBase.Length));
			buffer.Ref(urlBase.Length) = '/';
		}
	}

	/// <summary>
	/// Throws when the tag's endpoint doesn't take an ID in its path.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when <see cref="SupportsId"/> is false.</exception>
	private void ThrowIfNotSupportId()
	{
		if (!this.SupportsId)
		{
			throw new InvalidOperationException("This metadata tag does not support an ID in its path.");
		}
	}

	/// <summary>
	/// Returns a description of the tag, with its name and the cmdlets it can be piped to.
	/// </summary>
	/// <remarks>
	/// The first call builds the text and caches it, and later calls return the cached string.
	/// </remarks>
	/// <returns>
	/// The text <c>{Value = &lt;name&gt;, CanPipeTo = {&lt;cmdlets&gt;}}</c>, with the cmdlet names separated by
	/// a comma and a space.
	/// </returns>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
	public override string ToString()
	{
		return _toString ?? buildAndCache(this);

		// The local function caches the text itself, which leaves the call in tail position, so this method needs no
		// stack frame and the cached read is all it runs.
		[MethodImpl(MethodImplOptions.NoInlining)]
		static string buildAndCache(MetadataTag tag)
		{
			// {Value = <value>, CanPipeTo = {<comma[space]-separated list>}} <-- Format
			const string VALUE_START = "{" + nameof(Value) + " = ";
			const string PIPES_START = ", " + nameof(CanPipeTo) + " = {";
			const string SEPARATOR = ", ";
			const string END = "}}";

			ReadOnlySpan<string> pipesTo = tag.CanPipeTo.AsSpan();
			int length = VALUE_START.Length + tag.Value.Length + PIPES_START.Length + END.Length
				+ (SEPARATOR.Length * Math.Max(pipesTo.Length - 1, 0));

			foreach (string piped in pipesTo)
			{
				length += piped.Length;
			}

			using var builder = SpanStringBuilder.Create(
				length <= MAX_STACKALLOC
					? stackalloc char[length]
					: length);

			builder.Append(VALUE_START);
			builder.Append(tag.Value);
			builder.Append(PIPES_START);

			for (int i = 0; i < pipesTo.Length; i++)
			{
				if (i > 0)
				{
					builder.Append(SEPARATOR);
				}

				builder.Append(pipesTo[i]);
			}

			builder.Append(END);

			return tag._toString = builder.ToString();
		}
	}

	/// <summary>
	/// Determines whether two tags are equal.
	/// </summary>
	/// <param name="x">The first tag to compare.</param>
	/// <param name="y">The second tag to compare.</param>
	/// <returns>
	/// <see langword="true"/> if both are <see langword="null"/> or <paramref name="x"/> equals <paramref name="y"/>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public static bool operator ==(MetadataTag? x, MetadataTag? y)
	{
		return x is null ? y is null : x.Equals(y);
	}
	/// <summary>
	/// Determines whether two tags aren't equal.
	/// </summary>
	/// <param name="x">The first tag to compare.</param>
	/// <param name="y">The second tag to compare.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="x"/> doesn't equal <paramref name="y"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public static bool operator !=(MetadataTag? x, MetadataTag? y)
	{
		return !(x == y);
	}
}
