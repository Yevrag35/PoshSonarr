using MG.Sonarr.Resources;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Strings;

[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{Elapsed}")]
public readonly struct TimedValue : IFormattable
{
    private const double _exclusiveMaxMillisecondThreshold = 50_000;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly bool _useSeconds;

    /// <summary>
    /// Gets the <see cref="TimeSpan"/> value of the current instance.
    /// </summary>
    public readonly TimeSpan Elapsed;
    /// <summary>
    /// Gets a value indicating whether the formatted string output of <see cref="Elapsed"/> will be in seconds.
    /// </summary>
    public readonly bool WillUseSeconds => _useSeconds;

    [DebuggerStepThrough]
    public TimedValue()
    {
        Elapsed = TimeSpan.Zero;
        _useSeconds = false;
    }
    public TimedValue(TimeSpan elapsed)
    {
        Elapsed = elapsed;
        _useSeconds = elapsed.TotalMilliseconds >= _exclusiveMaxMillisecondThreshold;
    }

    [DebuggerStepThrough]
    public readonly double GetRoundedValue() => this.GetRoundedValue(decimalPlaces: 2);
    public readonly double GetRoundedValue(int decimalPlaces)
    {
        double value = _useSeconds ? Elapsed.TotalSeconds : Elapsed.TotalMilliseconds;
        return Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    public override readonly string ToString()
    {
        return this.ToString(format: null, provider: CultureInfo.CurrentCulture);
    }
    public readonly string ToString(string? format, IFormatProvider? provider)
    {
        double value = this.GetRoundedValue();
        string unit = !_useSeconds ? Messages.Timer_Unit_Milliseconds : Messages.Timer_Unit_Seconds;

        return Messenger.Format(
            provider,
            format: "{0}{1}",
            value,
            unit
        );
    }

    public static implicit operator TimeSpan(TimedValue value) => value.Elapsed;
    public static implicit operator TimedValue(TimeSpan value) => new(value);
}