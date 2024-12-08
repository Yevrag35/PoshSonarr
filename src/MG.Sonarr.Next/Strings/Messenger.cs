using System.Collections.Concurrent;
using System.Text;

namespace MG.Sonarr.Next.Strings;

public static class Messenger
{
    private static readonly Lazy<ConcurrentDictionary<string, CompositeFormat>> _formats = new(() =>
    {
        return new(Environment.ProcessorCount, 3, StringComparer.Ordinal);
    });

    public static string Format(string format, ReadOnlySpan<object?> arguments)
    {
        return Format(provider: CultureInfo.CurrentCulture, format, arguments);
    }
    public static string Format(IFormatProvider? provider, string format, ReadOnlySpan<object?> arguments)
    {
        provider ??= CultureInfo.CurrentCulture;
        CompositeFormat compositeFormat = GetCompositeFormat(format);
        return string.Format(
            provider,
            format: compositeFormat,
            args: arguments);
    }

    private static CompositeFormat GetCompositeFormat(string format)
    {
        return _formats.Value.GetOrAdd(format, key => CompositeFormat.Parse(key));
    }
}