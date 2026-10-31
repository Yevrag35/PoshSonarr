using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Services.Http.Queries;
using System.Collections.Immutable;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// Represents the tag that identifies which Sonarr API endpoint an object came from and which cmdlets it can be piped
/// to.
/// </summary>
/// <remarks>
/// <para>Instances are immutable. Get them from an <see cref="IMetadataResolver"/>, which creates one for each
/// endpoint when a connection is made.</para>
/// <para>The tag also builds the URLs of requests to its endpoint, from its <see cref="UrlBase"/>.</para>
/// </remarks>
[DebuggerDisplay(@"\{{Value}, {UrlBase}\}")]
public sealed class MetadataTag : ICloneable, IComparable<MetadataTag>, IEquatable<MetadataTag>
{
	/// <summary>
	/// The largest buffer, in characters, that <see cref="GetUrlForId{T}(T, QueryCol)"/> allocates on the stack instead
	/// of renting a pooled array.
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

	//BUG: It returns -1 for a null other, which puts null after every tag, where the IComparable<T> convention puts it first and returns 1.
	/// <summary>
	/// Compares the tag with another by their <see cref="Value"/>, ignoring case.
	/// </summary>
	/// <param name="other">The tag to compare with.</param>
	/// <returns>
	/// A negative number if this tag sorts before <paramref name="other"/>, 0 if they sort together, and a positive
	/// number if this tag sorts after it.
	/// </returns>
	public int CompareTo(MetadataTag? other)
	{
		if (this.IsNullOrReferenceEquals(other, out bool isEqual))
		{
			return isEqual ? 0 : -1;
		}

		return string.Compare(this.Value, other.Value, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Determines whether the tag equals another.
	/// </summary>
	/// <remarks>
	/// Two tags are equal when their <see cref="SupportsId"/> values match and their <see cref="UrlBase"/> and
	/// <see cref="Value"/> match, ignoring case. <see cref="CanPipeTo"/> isn't compared.
	/// </remarks>
	/// <param name="other">The tag to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="other"/> equals this tag; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Equals(MetadataTag? other)
	{
		if (!this.IsNullOrReferenceEquals(other, out bool isEqual))
		{
			isEqual = this.SupportsId == other.SupportsId
					  &&
					  this.UrlBase.Equals(other.UrlBase, StringComparison.OrdinalIgnoreCase)
					  &&
					  this.Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
		}

		return isEqual;
	}
	/// <summary>
	/// Determines whether the tag equals an object.
	/// </summary>
	/// <param name="obj">The object to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="obj"/> is a <see cref="MetadataTag"/> that equals this tag;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public override bool Equals(object? obj)
	{
		return obj is MetadataTag tag && this.Equals(tag);
	}
	//BUG: It hashes UrlBase and Value case-sensitively, while Equals ignores their case, so two equal tags can hash differently.
	/// <summary>
	/// Gets a hash code for the tag.
	/// </summary>
	/// <returns>A hash code of <see cref="UrlBase"/>, <see cref="Value"/>, and <see cref="SupportsId"/>.</returns>
	public override int GetHashCode()
	{
		return HashCode.Combine(this.UrlBase, this.Value, this.SupportsId);
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
	//BUG: It stack-allocates parameter.MaxLength + 1 characters with no cap, and a caller of QueryCol.Add can set any MaxLength, so a long field can overflow the stack.
	/// <summary>
	/// Builds the URL of a request to the tag's endpoint with a query string of one field.
	/// </summary>
	/// <param name="parameter">The query field to append.</param>
	/// <returns><see cref="UrlBase"/>, followed by <c>?</c> and the formatted field.</returns>
	public string GetUrl(IQueryField parameter)
	{
		Span<char> chars = stackalloc char[parameter.MaxLength + 1];
		chars[0] = '?';
		_ = parameter.TryFormat(chars[1..], out int written, default, null);

		return string.Concat(this.UrlBase, chars.Slice(0, written + 1));
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

	//BUG: When the ID doesn't fit in 40 characters, the fallback hits Debug.Fail and then copies the same text into the same too-short span, which throws.
	/// <summary>
	/// Builds the URL of a request for the item with the specified ID at the tag's endpoint.
	/// </summary>
	/// <remarks>
	/// The ID is formatted into a stack buffer of 40 characters, which holds any integer up to an <see cref="Int128"/>.
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

		if (!id.TryCopyToSlice(span, ref position))
		{
			Debug.Fail($"Unable to format '{id}' into the BaseUrl.");
			position = this.UrlBase.Length + 1;

			position = id.ToString(format: null, formatProvider: null)
						 .CopyToSlice(span, position);
		}

		return new string(span.Slice(0, position));
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

		builder.Append(id, LengthConstants.INT128_MAX);
		builder.Append(parameters, parameters.MaxLength);

		return builder.ToString();

		static void writeUrlBase(Span<char> buffer, string urlBase)
		{
			urlBase.CopyTo(buffer.UnsafeSlice(0, urlBase.Length));
			buffer.Ref(urlBase.Length) = '/';
		}
	}

	/// <summary>
	/// Determines whether another tag is null or this same instance, which settles a comparison without reading its
	/// values.
	/// </summary>
	/// <param name="other">The tag to check.</param>
	/// <param name="isEqual">
	/// When this method returns <see langword="true"/>, whether <paramref name="other"/> is this instance; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="other"/> is <see langword="null"/> or this instance; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private bool IsNullOrReferenceEquals([NotNullWhen(false)] MetadataTag? other, out bool isEqual)
	{
		if (other is null)
		{
			isEqual = false;
			return true;
		}
		else if (ReferenceEquals(this, other))
		{
			isEqual = true;
			return true;
		}

		isEqual = false;
		return false;
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
		if (_toString is not null)
			return _toString;

		// {Value = <value>; CanPipeTo = {<comma[space]-separated list}} <-- Format
		int length = this.Value.Length + nameof(Value).Length + nameof(CanPipeTo).Length;
		foreach (string piped in this.CanPipeTo.AsSpan())
		{
			length += piped.Length;
		}

		length += 12 + (2 * (this.CanPipeTo.Length - 1)); // 12 = the miscellaneous characters

		return _toString = string.Create(length, this, static (chars, state) =>
		{
			int position = 0;
			ReadOnlySpan<char> sep = [' ', '=', ' '];
			ReadOnlySpan<char> comma = [',', ' '];

			chars[position++] = '{';

			position = nameof(state.Value).CopyToSlice(chars, position);
			position = sep.CopyToSlice(chars, position);
			position = state.Value.CopyToSlice(chars, position);
			position = comma.CopyToSlice(chars, position);

			position = nameof(state.CanPipeTo).CopyToSlice(chars, position);
			position = sep.CopyToSlice(chars, position);
			chars[position++] = '{';

			int count = 0;
			foreach (string s in state.CanPipeTo)
			{
				position = s.CopyToSlice(chars, position);
				if (count < state.CanPipeTo.Length - 1)
				{
					position = comma.CopyToSlice(chars, position);
				}

				count++;
			}

			chars[position++] = '}';
			chars[position++] = '}';
		});
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
		return x.IsEqualTo<MetadataTag>(y);
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
