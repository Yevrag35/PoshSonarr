namespace MG.Sonarr.Next.Json.Converters.Spans;

/// <summary>
/// Provides a base class for converters that turn a property's JSON string value into an object, in place of the
/// <see cref="ObjectConverter"/>'s own parsing.
/// </summary>
/// <remarks>
/// The object converter picks a span converter by the property's name, after its first letter is uppercased and
/// it's renamed, and passes the unescaped characters of the value in a buffer that it owns.
/// </remarks>
public abstract class SpanConverter
{
	/// <summary>
	/// When overridden in a derived class, converts a property's string value into an object.
	/// </summary>
	/// <remarks>
	/// <paramref name="chars"/> is valid only for the length of the call, and a converter may change its characters.
	/// </remarks>
	/// <param name="chars">The unescaped characters of the value.</param>
	/// <param name="propertyName">The name of the property that the value belongs to.</param>
	/// <returns>The converted value, which becomes the property's value.</returns>
	public abstract object? ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName);
}

/// <summary>
/// Provides a base class for span converters that turn a property's string value into a <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type that the converter produces.</typeparam>
public abstract class SpanConverter<T> : SpanConverter
{
	/// <summary>
	/// Converts a property's string value into a <typeparamref name="T"/>, boxed when it's a value type.
	/// </summary>
	/// <param name="chars">The unescaped characters of the value.</param>
	/// <param name="propertyName">The name of the property that the value belongs to.</param>
	/// <returns>The converted value.</returns>
	public sealed override object? ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName)
	{
		return this.ConvertSpan(chars, propertyName, false);
	}

	/// <summary>
	/// When overridden in a derived class, converts a property's string value into a <typeparamref name="T"/>.
	/// </summary>
	/// <remarks>
	/// <paramref name="chars"/> is valid only for the length of the call, and a converter may change its characters.
	/// </remarks>
	/// <param name="chars">The unescaped characters of the value.</param>
	/// <param name="propertyName">The name of the property that the value belongs to.</param>
	/// <param name="isNull">
	/// A value that indicates whether the JSON value is null, which the non-generic
	/// <see cref="ConvertSpan(Span{char}, ReadOnlySpan{char})"/> always passes as <see langword="false"/>.
	/// </param>
	/// <returns>The converted value.</returns>
	public abstract T? ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, bool isNull);
}
