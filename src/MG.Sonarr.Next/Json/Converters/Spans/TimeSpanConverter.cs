namespace MG.Sonarr.Next.Json.Converters.Spans;

/// <summary>
/// Provides a span converter that reads a property's value as a <see cref="TimeSpan"/>, such as a <c>Duration</c>.
/// </summary>
/// <remarks>
/// The value is read with the invariant culture, as Sonarr writes it, whatever the current culture.
/// </remarks>
public sealed class TimeSpanConverter : SpanConverter<TimeSpan>
{
	/// <summary>
	/// Tries to convert a property's value into a <see cref="TimeSpan"/>.
	/// </summary>
	/// <param name="chars">The unescaped characters of the value, such as <c>00:42:10</c>.</param>
	/// <param name="propertyName">The name of the property that the value belongs to, which isn't used.</param>
	/// <param name="value">
	/// When this method returns <see langword="true"/>, the parsed time interval; otherwise, <see cref="TimeSpan.Zero"/>.
	/// </param>
	/// <returns><see langword="true"/> if the value parsed as a time interval; otherwise, <see langword="false"/>.</returns>
	public override bool TryConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, out TimeSpan value)
	{
		return TimeSpan.TryParse(chars, CultureInfo.InvariantCulture, out value);
	}
}
