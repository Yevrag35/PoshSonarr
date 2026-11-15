namespace MG.Sonarr.Next.Json.Converters.Spans;

/// <summary>
/// Provides a span converter that reads a property's value as a <see cref="TimeOnly"/>, such as a series'
/// <c>AirTime</c>.
/// </summary>
/// <remarks>
/// The value is read with the invariant culture, as Sonarr writes it, whatever the current culture.
/// </remarks>
public sealed class TimeOnlyConverter : SpanConverter<TimeOnly>
{
	/// <summary>
	/// Tries to convert a property's value into a <see cref="TimeOnly"/>.
	/// </summary>
	/// <remarks>
	/// A value that holds only a time, such as <c>21:00</c>, is read as that time. A value that holds a date and a
	/// time is read as a <see cref="DateTimeOffset"/>, with UTC assumed when it has no offset, and its time of day in
	/// UTC is taken.
	/// </remarks>
	/// <param name="chars">The unescaped characters of the value.</param>
	/// <param name="propertyName">The name of the property that the value belongs to, which isn't used.</param>
	/// <param name="value">When this method returns <see langword="true"/>, the parsed time; otherwise, midnight.</param>
	/// <returns>
	/// <see langword="true"/> if the value parsed as a time or as a date and time; otherwise, <see langword="false"/>.
	/// </returns>
	public override bool TryConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, out TimeOnly value)
	{
		// TimeOnly takes only white-space styles, and returns false for AssumeUniversal.
		if (TimeOnly.TryParse(chars, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
		{
			return true;
		}
		else if (DateTimeOffset.TryParse(chars, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset tempOff))
		{
			value = TimeOnly.FromDateTime(tempOff.UtcDateTime);
			return true;
		}

		value = default;
		return false;
	}
}

/// <summary>
/// Provides a span converter that reads a property's value as a <see cref="DateOnly"/>, such as an episode's
/// <c>AirDate</c> or a series' <c>FirstAired</c>.
/// </summary>
/// <remarks>
/// The value is read with the invariant culture, as Sonarr writes it, whatever the current culture, so a date such
/// as <c>2024-01-15</c> is a Gregorian date under a culture whose calendar is another, such as th-TH's Buddhist one.
/// </remarks>
public sealed class DateOnlyConverter : SpanConverter<DateOnly>
{
	/// <summary>
	/// Tries to convert a property's value into a <see cref="DateOnly"/>.
	/// </summary>
	/// <remarks>
	/// A value that holds only a date, such as <c>2024-01-15</c>, is read as that date. A value that holds a date and
	/// a time is read as a <see cref="DateTimeOffset"/>, with UTC assumed when it has no offset, and the date at its
	/// own offset is taken.
	/// </remarks>
	/// <param name="chars">The unescaped characters of the value.</param>
	/// <param name="propertyName">The name of the property that the value belongs to, which isn't used.</param>
	/// <param name="value">
	/// When this method returns <see langword="true"/>, the parsed date; otherwise, <see cref="DateOnly.MinValue"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the value parsed as a date or as a date and time; otherwise, <see langword="false"/>.
	/// </returns>
	public override bool TryConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, out DateOnly value)
	{
		// DateOnly takes only white-space styles, and returns false for AssumeUniversal.
		if (DateOnly.TryParse(chars, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
		{
			return true;
		}
		else if (DateTimeOffset.TryParse(chars, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset tempOff))
		{
			value = DateOnly.FromDateTime(tempOff.DateTime);
			return true;
		}

		value = default;
		return false;
	}
}
