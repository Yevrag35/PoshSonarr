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
/// <remarks>
/// A value that the converter can't read as a <typeparamref name="T"/> stays the string that it came as, so text in a
/// form that the converter doesn't know reaches PowerShell, and goes back in a request body, as Sonarr sent it, rather
/// than as a default value.
/// </remarks>
/// <typeparam name="T">The type that the converter produces.</typeparam>
public abstract class SpanConverter<T> : SpanConverter
{
	/// <summary>
	/// Converts a property's string value into a <typeparamref name="T"/>, boxed when it's a value type, or keeps it a
	/// string when the converter can't read it.
	/// </summary>
	/// <param name="chars">The unescaped characters of the value.</param>
	/// <param name="propertyName">The name of the property that the value belongs to.</param>
	/// <returns>
	/// The converted value, or the value's characters as a string when <see cref="TryConvertSpan"/> returns
	/// <see langword="false"/>.
	/// </returns>
	public sealed override object? ConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName)
	{
		return this.TryConvertSpan(chars, propertyName, out T? value)
			? value
			: new string(chars);
	}

	/// <summary>
	/// When overridden in a derived class, tries to convert a property's string value into a <typeparamref name="T"/>.
	/// </summary>
	/// <remarks>
	/// <paramref name="chars"/> is valid only for the length of the call. A converter may change its characters when it
	/// returns <see langword="true"/>, and leaves them as they came when it returns <see langword="false"/>, since they
	/// then become the property's value.
	/// </remarks>
	/// <param name="chars">The unescaped characters of the value.</param>
	/// <param name="propertyName">The name of the property that the value belongs to.</param>
	/// <param name="value">
	/// When this method returns <see langword="true"/>, the converted value; otherwise, the default value of
	/// <typeparamref name="T"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the converter read the value as a <typeparamref name="T"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public abstract bool TryConvertSpan(Span<char> chars, ReadOnlySpan<char> propertyName, [MaybeNullWhen(false)] out T value);
}
