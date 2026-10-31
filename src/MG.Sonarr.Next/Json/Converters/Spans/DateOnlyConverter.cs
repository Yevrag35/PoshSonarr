namespace MG.Sonarr.Next.Json.Converters.Spans;

//BUG: TimeOnly.TryParse rejects DateTimeStyles.AssumeUniversal and always returns false, so every value falls through to the DateTimeOffset parse.
/// <summary>
/// Provides a span converter that reads a property's value as a <see cref="TimeOnly"/>, such as a series'
/// <c>AirTime</c>.
/// </summary>
public sealed class TimeOnlyConverter : SpanConverter<TimeOnly>
{
	/// <summary>
	/// Converts a property's value into a <see cref="TimeOnly"/>.
	/// </summary>
	/// <remarks>
	/// A value that holds only a time, such as <c>21:00</c>, is read as that time. A value that holds a date and a
	/// time is read as a <see cref="DateTimeOffset"/>, with UTC assumed when it has no offset, and its time of day in
	/// UTC is taken.
	/// </remarks>
	/// <param name="chars">The unescaped characters of the value.</param>
	/// <param name="propertyName">The name of the property that the value belongs to, which isn't used.</param>
	/// <param name="isNull">A value that indicates whether the JSON value is null, which isn't used.</param>
	/// <returns>The parsed time, or midnight when the value doesn't parse.</returns>
	public override TimeOnly ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull)
	{
		if (TimeOnly.TryParse(chars, Statics.DefaultProvider, DateTimeStyles.AssumeUniversal, out TimeOnly @to))
		{
			return @to;
		}
		else if (DateTimeOffset.TryParse(chars, Statics.DefaultProvider, DateTimeStyles.AssumeUniversal, out DateTimeOffset tempOff))
		{
			return TimeOnly.FromDateTime(tempOff.UtcDateTime);
		}

		return default;
	}
}

//BUG: DateOnly.TryParse rejects DateTimeStyles.AssumeUniversal and always returns false, so every value falls through to the DateTimeOffset parse.
/// <summary>
/// Provides a span converter that reads a property's value as a <see cref="DateOnly"/>, such as an episode's
/// <c>AirDate</c> or a series' <c>FirstAired</c>.
/// </summary>
public sealed class DateOnlyConverter : SpanConverter<DateOnly>
{
	/// <summary>
	/// Converts a property's value into a <see cref="DateOnly"/>.
	/// </summary>
	/// <remarks>
	/// A value that holds only a date, such as <c>2024-01-15</c>, is read as that date. A value that holds a date and
	/// a time is read as a <see cref="DateTimeOffset"/>, with UTC assumed when it has no offset, and the date at its
	/// own offset is taken.
	/// </remarks>
	/// <param name="chars">The unescaped characters of the value.</param>
	/// <param name="propertyName">The name of the property that the value belongs to, which isn't used.</param>
	/// <param name="isNull">A value that indicates whether the JSON value is null, which isn't used.</param>
	/// <returns>The parsed date, or <see cref="DateOnly.MinValue"/> when the value doesn't parse.</returns>
	public override DateOnly ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull)
	{
		if (DateOnly.TryParse(chars, Statics.DefaultProvider, DateTimeStyles.AssumeUniversal, out DateOnly @do))
		{
			return @do;
		}
		else if (DateTimeOffset.TryParse(chars, Statics.DefaultProvider, DateTimeStyles.AssumeUniversal, out DateTimeOffset tempOff))
		{
			return DateOnly.FromDateTime(tempOff.DateTime);
		}

		return @do;
	}
}
