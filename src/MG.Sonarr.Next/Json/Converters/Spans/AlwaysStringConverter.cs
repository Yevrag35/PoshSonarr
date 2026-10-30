namespace MG.Sonarr.Next.Json.Converters.Spans;

public sealed class AlwaysStringConverter : SpanConverter<string>
{
	readonly bool _trim;

	public AlwaysStringConverter()
		: this(trim: true)
	{
	}
	public AlwaysStringConverter(bool trim)
	{
		_trim = trim;
	}

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
