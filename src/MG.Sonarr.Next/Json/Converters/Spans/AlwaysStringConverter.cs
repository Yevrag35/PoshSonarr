namespace MG.Sonarr.Next.Json.Converters.Spans;

/// <summary>
/// Provides a span converter that keeps a property's value a string, so that no number, <see cref="Guid"/>, date, or
/// <see cref="Version"/> parsing claims it.
/// </summary>
/// <remarks>
/// It's for values such as titles, labels, hashes, and file modes, which Sonarr expects back as strings, and,
/// without trimming, for credentials, which go back exactly as they came.
/// </remarks>
public sealed class AlwaysStringConverter : SpanConverter<string>
{
	/// <summary>
	/// A value that indicates whether white space is trimmed from both ends of a value.
	/// </summary>
	readonly bool _trim;

	/// <summary>
	/// Initializes a new <see cref="AlwaysStringConverter"/> instance that trims white space from both ends of a value.
	/// </summary>
	public AlwaysStringConverter()
		: this(trim: true)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="AlwaysStringConverter"/> instance that trims white space from both ends of a value
	/// or keeps it, as specified.
	/// </summary>
	/// <param name="trim">
	/// <see langword="true"/> to trim white space from both ends of a value; <see langword="false"/> to keep the value
	/// exactly as it came.
	/// </param>
	public AlwaysStringConverter(bool trim)
	{
		_trim = trim;
	}

	/// <summary>
	/// Converts a property's value into a string.
	/// </summary>
	/// <param name="chars">The unescaped characters of the value.</param>
	/// <param name="propertyName">The name of the property that the value belongs to, which isn't used.</param>
	/// <param name="isNull">A value that indicates whether the JSON value is null, which isn't used.</param>
	/// <returns>
	/// The value as a string, trimmed when the converter trims, or <see cref="string.Empty"/> when nothing remains.
	/// </returns>
	public override string? ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull)
	{
		if (_trim)
		{
			chars = chars.Trim();
		}

		return chars.IsEmpty
			? string.Empty
			: new string(chars);
	}
}
