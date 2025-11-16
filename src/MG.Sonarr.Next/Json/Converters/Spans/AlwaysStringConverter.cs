namespace MG.Sonarr.Next.Json.Converters.Spans;

public sealed class AlwaysStringConverter : SpanConverter<string>
{
	public override string? ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull)
	{
		chars = chars.Trim();
		return chars.IsEmpty
			? string.Empty
			: new string(chars);
	}
}
