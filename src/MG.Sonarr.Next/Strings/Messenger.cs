using System.Collections.Concurrent;

namespace MG.Sonarr.Next.Strings;

/// <summary>
/// Provides cached composite formatting helpers.
/// </summary>
public static class Messenger
{
	private static readonly Lazy<ConcurrentDictionary<string, CompositeFormat>> _formats = new(() =>
	{
		return new(Environment.ProcessorCount, 3, StringComparer.Ordinal);
	});

	/// <summary>
	/// Formats a composite string by using the current culture.
	/// </summary>
	/// <param name="format">The composite format string.</param>
	/// <param name="arguments">The format arguments.</param>
	/// <returns>The formatted string result.</returns>
	public static string Format(string format, params ReadOnlySpan<object?> arguments)
	{
		return Format(provider: CultureInfo.CurrentCulture, format, arguments);
	}

	/// <summary>
	/// Formats a composite string by using the specified format provider.
	/// </summary>
	/// <param name="provider">The format provider for value formatting, or <see langword="null"/> to use the current culture.</param>
	/// <param name="format">The composite format string.</param>
	/// <param name="arguments">The format arguments.</param>
	/// <returns>The formatted string result.</returns>
	public static string Format(IFormatProvider? provider, string format, params ReadOnlySpan<object?> arguments)
	{
		provider ??= CultureInfo.CurrentCulture;
		CompositeFormat compositeFormat = GetCompositeFormat(format);
		return string.Format(
			provider,
			format: compositeFormat,
			args: arguments);
	}

	/// <summary>
	/// Gets a cached parsed composite format for the specified format string.
	/// </summary>
	/// <param name="format">The composite format string to parse or retrieve.</param>
	/// <returns>The cached parsed composite format.</returns>
	private static CompositeFormat GetCompositeFormat(string format)
	{
		return _formats.Value.GetOrAdd(format, CompositeFormat.Parse);
	}
}