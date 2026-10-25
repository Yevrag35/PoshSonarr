using MG.Sonarr.Resources;

namespace MG.Sonarr.Next.Strings;

[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{Elapsed}")]
public readonly struct TimedValue : IFormattable
{
	private const double _exclusiveMaxMillisecondThreshold = 50_000;

	/// <summary>
	/// Gets the <see cref="TimeSpan"/> value of the current instance.
	/// </summary>
	public TimeSpan Elapsed { get; }
	/// <summary>
	/// Gets a value indicating whether the formatted string output of <see cref="Elapsed"/> will be in seconds.
	/// </summary>
	public readonly bool WillUseSeconds => this.Elapsed.TotalMilliseconds >= _exclusiveMaxMillisecondThreshold;

	[DebuggerStepThrough]
	public TimedValue()
	{
		this.Elapsed = TimeSpan.Zero;
	}
	public TimedValue(TimeSpan elapsed)
	{
		this.Elapsed = elapsed;
	}

	[DebuggerStepThrough]
	public readonly double GetRoundedValue() => this.GetRoundedValue(decimalPlaces: 2);
	public readonly double GetRoundedValue(int decimalPlaces)
	{
		double value = this.WillUseSeconds ? this.Elapsed.TotalSeconds : this.Elapsed.TotalMilliseconds;
		return Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
	}

	public override readonly string ToString()
	{
		return this.ToString(format: null, provider: CultureInfo.CurrentCulture);
	}
	public readonly string ToString(string? format, IFormatProvider? provider)
	{
		double value = this.GetRoundedValue();
		string unit = !this.WillUseSeconds ? Messages.Timer_Unit_Milliseconds : Messages.Timer_Unit_Seconds;

		Span<char> buffer = stackalloc char[LengthConstants.DOUBLE_MAX];
		_ = value.TryFormat(buffer, out int written);

		return string.Concat(buffer.Slice(0, written), unit);
	}

	public static implicit operator TimeSpan(TimedValue value) => value.Elapsed;
	public static implicit operator TimedValue(TimeSpan value) => new(value);
}