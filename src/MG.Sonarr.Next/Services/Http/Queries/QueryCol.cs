using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Strings;

namespace MG.Sonarr.Next.Services.Http.Queries;

/// <summary>
/// Represents a collection of query string fields, kept sorted by key, that formats as a URL query
/// string such as <c>?key1=value1&amp;key2=value2</c>.
/// </summary>
/// <remarks>
/// <para>
///     Each key appears once, compared by ordinal, case-insensitive comparison, and the fields are
///     written in key order. The collection keeps a running total of its fields' maximum lengths,
///     so <see cref="MaxLength"/> sizes a buffer for the whole query string without formatting it.
/// </para>
/// <para>
///     Keys and values are written as given, with no URL escaping. The collection isn't
///     thread-safe.
/// </para>
/// </remarks>
[DebuggerDisplay(@"\{Count = {Count}, MaxLength = {MaxLength}\}")]
public sealed class QueryCol : IReadOnlyList<IQueryField>, ISpanFormattable
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private int _maxLength;

	private readonly SortedList<string, IQueryField> _fields;

	/// <summary>
	/// Gets or sets the field at the specified index in key order.
	/// </summary>
	/// <param name="index">The zero-based index of the field.</param>
	/// <value>The field at <paramref name="index"/>.</value>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative or isn't less than <see cref="Count"/>.</exception>
	public IQueryField this[int index]
	{
		get => _fields.Values[index];
		set => _fields.Values[index] = value;
	}

	/// <summary>
	/// Gets the number of fields in the collection.
	/// </summary>
	/// <value>The number of fields.</value>
	public int Count => _fields.Count;
	/// <summary>
	/// Gets the largest number of characters that the collection writes when formatted.
	/// </summary>
	/// <value>
	///     The sum of the fields' maximum lengths, plus one for the <c>?</c> and one for each
	///     <c>&amp;</c> between two fields.
	/// </value>
	public int MaxLength => _maxLength + 1 + Math.Max(0, this.Count - 1);

	/// <summary>
	/// Initializes a new, empty <see cref="QueryCol"/> instance with the default capacity.
	/// </summary>
	[DebuggerStepThrough]
	public QueryCol()
	{
		_fields = [];
	}
	/// <summary>
	/// Initializes a new, empty <see cref="QueryCol"/> instance with room for the specified number
	/// of fields.
	/// </summary>
	/// <param name="capacity">The number of fields that the collection holds before it grows.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is negative.</exception>
	public QueryCol(int capacity)
	{
		_fields = new(capacity, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Adds a field that writes the specified Boolean value as <c>key=true</c> or <c>key=false</c>.
	/// </summary>
	/// <param name="value">The value that the field writes.</param>
	/// <param name="key">
	///     The field's key. When it's omitted, the compiler supplies the text of the expression passed
	///     as <paramref name="value"/>, such as <c>includeSeries</c>.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters, or when the collection already holds a field with that key.</exception>
	public void AddBoolean(bool value, [CallerArgumentExpression(nameof(value))] string key = "")
	{
		this.Add([key, value]);
	}
	/// <summary>
	/// Adds a field that writes <c>key=true</c> when the specified value is <see langword="true"/>,
	/// and adds nothing otherwise.
	/// </summary>
	/// <param name="value">The value that decides whether the field is added.</param>
	/// <param name="key">
	///     The field's key. When it's omitted, the compiler supplies the text of the expression passed
	///     as <paramref name="value"/>, such as <c>includeSeries</c>.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is true and <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is true and <paramref name="key"/> is empty or consists only of white-space characters, or when the collection already holds a field with that key.</exception>
	public void AddIfTrue(bool value, [CallerArgumentExpression(nameof(value))] string key = "")
	{
		if (value)
		{
			_fields.Add(key, BooleanQueryField.CreateTrue(key));
			_maxLength += key.Length + 1 + bool.TrueString.Length;
		}
	}
	/// <summary>
	/// Adds the specified Boolean fields.
	/// </summary>
	/// <remarks>
	/// The fields are added in order, so when one throws, the fields before it stay in the
	/// collection. A default <see cref="BooleanQueryField"/> is added under an empty key and makes
	/// formatting the collection fail.
	/// </remarks>
	/// <param name="fields">The fields to add.</param>
	/// <exception cref="ArgumentException">Thrown when the collection already holds a field with the key of one of <paramref name="fields"/>, or when two of them share a key.</exception>
	public void Add(params ReadOnlySpan<BooleanQueryField> fields)
	{
		foreach (BooleanQueryField field in fields)
		{
			_fields.Add(field.Key, field);
			_maxLength += field.MaxLength;
		}
	}
	/// <summary>
	/// Adds a field that writes the specified 32-bit integer as <c>key=value</c>.
	/// </summary>
	/// <param name="key">The field's key.</param>
	/// <param name="value">The value that the field writes.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters, or when the collection already holds a field with that key.</exception>
	[DebuggerStepThrough]
	public void Add(string key, int value)
	{
		this.Add(key, value, LengthConstants.INT_MAX);
	}
	/// <summary>
	/// Adds a field that writes the specified 64-bit integer as <c>key=value</c>.
	/// </summary>
	/// <param name="key">The field's key.</param>
	/// <param name="value">The value that the field writes.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters, or when the collection already holds a field with that key.</exception>
	[DebuggerStepThrough]
	public void Add(string key, long value)
	{
		this.Add(key, value, LengthConstants.LONG_MAX);
	}
	/// <summary>
	/// Adds a field that writes the specified string as <c>key=value</c>, or the key alone when the
	/// string is <see langword="null"/>, empty, or white space.
	/// </summary>
	/// <param name="key">The field's key.</param>
	/// <param name="value">The value that the field writes, as given.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters, or when the collection already holds a field with that key.</exception>
	public void Add(string key, string? value)
	{
		IQueryField field = string.IsNullOrWhiteSpace(value)
			? new KeyOnlyQueryField(key)
			: new FormattableQueryField(key, value);

		this.Add(field);
	}
	/// <summary>
	/// Adds a field that formats the specified value as <c>key=value</c>, with an optional fixed
	/// format.
	/// </summary>
	/// <param name="key">The field's key.</param>
	/// <param name="value">The value that the field formats each time the collection is formatted.</param>
	/// <param name="maxLength">The largest number of characters that formatting <paramref name="value"/> produces.</param>
	/// <param name="format">The format that <paramref name="value"/> is formatted with, or <see langword="null"/> for its default format.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters, or when the collection already holds a field with that key.</exception>
	public void Add(string key, ISpanFormattable value, int maxLength, string? format = null)
	{
		FormattableQueryField field = new(key, value, maxLength, format);
		this.Add(field);
	}
	/// <summary>
	/// Adds the specified field.
	/// </summary>
	/// <param name="field">The field to add.</param>
	/// <exception cref="ArgumentException">Thrown when the collection already holds a field with the key of <paramref name="field"/>.</exception>
	public void Add(IQueryField field)
	{
		_fields.Add(field.Key, field);
		_maxLength += field.MaxLength;
	}
	/// <summary>
	/// Adds a field that writes the specified 32-bit integer as <c>key=value</c>, or replaces the
	/// field that already has the key.
	/// </summary>
	/// <param name="key">The field's key.</param>
	/// <param name="value">The value that the field writes.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters.</exception>
	public void AddOrUpdate(string key, int value)
	{
		FormattableQueryField field = new(key, value, LengthConstants.INT_MAX);
		this.AddOrUpdate(field);
	}
	/// <summary>
	/// Adds a field that writes the specified 64-bit integer as <c>key=value</c>, or replaces the
	/// field that already has the key.
	/// </summary>
	/// <param name="key">The field's key.</param>
	/// <param name="value">The value that the field writes.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters.</exception>
	public void AddOrUpdate(string key, long value)
	{
		FormattableQueryField field = new(key, value, LengthConstants.LONG_MAX);
		this.AddOrUpdate(field);
	}
	/// <summary>
	/// Adds a field that writes the specified string as <c>key=value</c>, or the key alone when the
	/// string is <see langword="null"/>, empty, or white space, or replaces the field that already
	/// has the key.
	/// </summary>
	/// <param name="key">The field's key.</param>
	/// <param name="value">The value that the field writes, as given.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters.</exception>
	public void AddOrUpdate(string key, string? value)
	{
		IQueryField field = string.IsNullOrWhiteSpace(value)
			? new KeyOnlyQueryField(key)
			: new FormattableQueryField(key, value);
		this.AddOrUpdate(field);
	}
	/// <summary>
	/// Adds the specified field, or replaces the field that already has its key.
	/// </summary>
	/// <param name="field">The field to add or to replace the existing one with.</param>
	public void AddOrUpdate(IQueryField field)
	{
		int index = _fields.IndexOfKey(field.Key);
		switch (index)
		{
			case -1:
				this.Add(field);
				break;

			case > -1 when index < _fields.Count:
				IQueryField existing = _fields.GetValueAtIndex(index);
				_maxLength += field.MaxLength - existing.MaxLength;
				_fields[field.Key] = field;
				break;

			default:
				throw new InvalidOperationException("Invalid index.");
		}
	}
	/// <summary>
	/// Removes every field from the collection.
	/// </summary>
	[DebuggerStepThrough]
	public void Clear()
	{
		_fields.Clear();
		_maxLength = 0;
	}
	/// <summary>
	/// Removes the field with the specified key.
	/// </summary>
	/// <param name="key">The key of the field to remove.</param>
	/// <returns>
	///     <see langword="true"/> if the collection held a field with <paramref name="key"/>;
	///     otherwise, <see langword="false"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	public bool Remove(string key)
	{
		if (_fields.Remove(key, out IQueryField? field))
		{
			_maxLength -= field.MaxLength;
			return true;
		}

		return false;
	}
	/// <summary>
	/// Removes the field at the specified index in key order.
	/// </summary>
	/// <param name="index">The zero-based index of the field to remove.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative or isn't less than <see cref="Count"/>.</exception>
	public void RemoveAt(int index)
	{
		IQueryField field = _fields.GetValueAtIndex(index);
		_maxLength -= field.MaxLength;
		_fields.RemoveAt(index);
	}

	/// <summary>
	/// Tries to write the collection as a query string, <c>?</c> followed by the fields in key order
	/// joined by <c>&amp;</c>, into the specified span.
	/// </summary>
	/// <remarks>
	/// Numbers and dates are formatted with the invariant culture, so the query string reads the
	/// same whatever the current culture is.
	/// </remarks>
	/// <param name="destination">The span to write the query string into.</param>
	/// <param name="charsWritten">
	///     When this method returns, the number of characters written to
	///     <paramref name="destination"/>, which counts the fields that fit before one didn't.
	/// </param>
	/// <returns>
	///     <see langword="true"/> if the collection is empty, which writes nothing, or if the whole
	///     query string fits in <paramref name="destination"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryFormat(Span<char> destination, out int charsWritten)
	{
		charsWritten = 0;
		if (this.Count == 0)
		{
			return true;
		}

		destination[charsWritten++] = '?';

		IQueryField field = _fields.GetValueAtIndex(0);
		if (!field.TryCopyToSlice(destination, ref charsWritten))
		{
			return false;
		}

		for (int i = 1; i < _fields.Count; i++)
		{
			destination[charsWritten++] = '&';
			field = _fields.GetValueAtIndex(i);
			if (!field.TryCopyToSlice(destination, ref charsWritten))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Returns the collection formatted as a query string.
	/// </summary>
	/// <param name="format">Ignored; the collection has one format.</param>
	/// <param name="formatProvider">Ignored; see <see cref="TryFormat(Span{char}, out int)"/>.</param>
	/// <returns>
	///     The query string, an empty string for an empty collection, or the fields that fit when one
	///     can't be formatted.
	/// </returns>
	string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
	{
		Span<char> chars = stackalloc char[_maxLength + 1 + (Math.Max(0, this.Count - 1))];
		_ = this.TryFormat(chars, out int charsWritten);

		return new string(chars.Slice(0, charsWritten));
	}
	/// <summary>
	/// Tries to write the collection as a query string into the specified span.
	/// </summary>
	/// <param name="destination">The span to write the query string into.</param>
	/// <param name="charsWritten">When this method returns, the number of characters written to <paramref name="destination"/>.</param>
	/// <param name="format">Ignored; the collection has one format.</param>
	/// <param name="provider">Ignored; see <see cref="TryFormat(Span{char}, out int)"/>.</param>
	/// <returns>
	///     <see langword="true"/> if the collection is empty or the whole query string fits in
	///     <paramref name="destination"/>; otherwise, <see langword="false"/>.
	/// </returns>
	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		return this.TryFormat(destination, out charsWritten);
	}

	/// <summary>
	/// Returns an enumerator that iterates through the fields in key order.
	/// </summary>
	/// <returns>An enumerator over the collection's fields.</returns>
	[DebuggerStepThrough]
	public IEnumerator<IQueryField> GetEnumerator()
	{
		return _fields.Values.GetEnumerator();
	}
	/// <summary>
	/// Returns an enumerator that iterates through the fields in key order.
	/// </summary>
	/// <returns>An enumerator over the collection's fields.</returns>
	[DebuggerStepThrough]
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}
}

/// <summary>
/// Provides extension methods for <see cref="QueryCol"/>.
/// </summary>
public static class QueryColExtensions
{
	/// <summary>
	/// Returns the specified URL with the collection's query string appended.
	/// </summary>
	/// <remarks>
	/// The URL is built on the stack when it's 256 characters or fewer, and in a pooled buffer
	/// otherwise, so the only allocation is the returned string.
	/// </remarks>
	/// <param name="collection">The query fields to append, or <see langword="null"/>.</param>
	/// <param name="baseUrl">The URL, without a query string, to append the fields to.</param>
	/// <returns>
	///     <paramref name="baseUrl"/> followed by the query string; or <paramref name="baseUrl"/>
	///     itself when <paramref name="collection"/> is <see langword="null"/> or empty.
	/// </returns>
	/// <exception cref="FormatException">Thrown when the fields of <paramref name="collection"/> don't fit in its <see cref="QueryCol.MaxLength"/>.</exception>
	public static string GetUrl(this QueryCol? collection, string baseUrl)
	{
		const int max_stackalloc = 256;

		if (collection is null || collection.Count == 0)
		{
			return baseUrl;
		}

		int length = baseUrl.Length + collection.MaxLength + 1;
		using var builder = SpanStringBuilder.Create(
			length <= max_stackalloc
				? stackalloc char[length]
				: length);

		builder.Append(baseUrl);
		builder.Append(collection, collection.MaxLength);

		return builder.ToString();
	}
}
