using MG.Sonarr.Resources;

namespace MG.Sonarr.Next.Strings;

/// <summary>
/// Represents an elapsed duration with formatted output in milliseconds or seconds.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{Elapsed}")]
public readonly struct TimedValue : IFormattable
{
	private const double _exclusiveMaxMillisecondThreshold = 50_000;

	/// <summary>
	/// Gets the <see cref="TimeSpan"/> value of the current instance.
	/// </summary>
	/// <value>The elapsed duration value.</value>
	public TimeSpan Elapsed { get; }

	/// <summary>
	/// Gets a value indicating whether the formatted string output of <see cref="Elapsed"/> will be in seconds.
	/// </summary>
	/// <value><see langword="true"/> when output uses seconds; otherwise, <see langword="false"/>.</value>
	public readonly bool WillUseSeconds => this.Elapsed.TotalMilliseconds >= _exclusiveMaxMillisecondThreshold;

	/// <summary>
	/// Initializes a new <see cref="TimedValue"/> instance with a zero elapsed duration.
	/// </summary>
	[DebuggerStepThrough]
	public TimedValue()
	{
		this.Elapsed = TimeSpan.Zero;
	}

	/// <summary>
	/// Initializes a new <see cref="TimedValue"/> instance with the specified elapsed duration.
	/// </summary>
	/// <param name="elapsed">The elapsed duration.</param>
	public TimedValue(TimeSpan elapsed)
	{
		this.Elapsed = elapsed;
	}

	/// <summary>
	/// Gets the rounded elapsed value using two decimal places.
	/// </summary>
	/// <returns>The rounded elapsed value in milliseconds or seconds, based on <see cref="WillUseSeconds"/>.</returns>
	[DebuggerStepThrough]
	public readonly double GetRoundedValue() => this.GetRoundedValue(decimalPlaces: 2);

	/// <summary>
	/// Gets the rounded elapsed value using the specified number of decimal places.
	/// </summary>
	/// <param name="decimalPlaces">The number of fractional digits in the rounded result.</param>
	/// <returns>The rounded elapsed value in milliseconds or seconds, based on <see cref="WillUseSeconds"/>.</returns>
	public readonly double GetRoundedValue(int decimalPlaces)
	{
		double value = this.WillUseSeconds ? this.Elapsed.TotalSeconds : this.Elapsed.TotalMilliseconds;
		return Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
	}

	/// <summary>
	/// Returns the string representation of this instance using the current culture.
	/// </summary>
	/// <returns>The formatted elapsed value with its unit suffix.</returns>
	public override readonly string ToString()
	{
		return this.ToString(format: null, provider: CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Returns the string representation of this instance using the specified format provider.
	/// </summary>
	/// <param name="format">The format string. This value is currently ignored.</param>
	/// <param name="provider">The format provider used for numeric formatting.</param>
	/// <returns>The formatted elapsed value with its unit suffix.</returns>
	public readonly string ToString(string? format, IFormatProvider? provider)
	{
		double value = this.GetRoundedValue();
		string unit = !this.WillUseSeconds ? Messages.Timer_Unit_Milliseconds : Messages.Timer_Unit_Seconds;

		Span<char> buffer = stackalloc char[LengthConstants.DOUBLE_MAX];
		_ = value.TryFormat(buffer, out int written, provider: provider);

		return string.Concat(buffer.Slice(0, written), unit);
	}

	/// <summary>
	/// Converts a <see cref="TimedValue"/> instance to its underlying <see cref="TimeSpan"/> value.
	/// </summary>
	/// <param name="value">The timed value to convert.</param>
	/// <returns>The underlying elapsed duration.</returns>
	public static implicit operator TimeSpan(TimedValue value) => value.Elapsed;

	/// <summary>
	/// Converts a <see cref="TimeSpan"/> value to a <see cref="TimedValue"/> instance.
	/// </summary>
	/// <param name="value">The elapsed duration to convert.</param>
	/// <returns>A timed value for the specified duration.</returns>
	public static implicit operator TimedValue(TimeSpan value) => new(value);
}