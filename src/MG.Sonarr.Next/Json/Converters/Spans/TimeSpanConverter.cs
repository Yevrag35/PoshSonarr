namespace MG.Sonarr.Next.Json.Converters.Spans;

/// <summary>
/// Provides a span converter that reads a property's value as a <see cref="TimeSpan"/>, such as a <c>Duration</c>.
/// </summary>
public sealed class TimeSpanConverter : SpanConverter<TimeSpan>
{
	/// <summary>
	/// Converts a property's value into a <see cref="TimeSpan"/>.
	/// </summary>
	/// <param name="chars">The unescaped characters of the value, such as <c>00:42:10</c>.</param>
	/// <param name="propertyName">The name of the property that the value belongs to, which isn't used.</param>
	/// <param name="isNull">A value that indicates whether the JSON value is null, which isn't used.</param>
	/// <returns>The parsed time interval, or <see cref="TimeSpan.Zero"/> when the value doesn't parse.</returns>
	public override TimeSpan ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull)
	{
		if (TimeSpan.TryParse(chars, Statics.DefaultProvider, out TimeSpan timeSpan))
		{
			return timeSpan;
		}

		return default;
	}
}
