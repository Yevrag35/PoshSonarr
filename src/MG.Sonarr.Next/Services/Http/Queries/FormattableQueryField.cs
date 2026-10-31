using MG.Sonarr.Next.Extensions.Strings;

namespace MG.Sonarr.Next.Services.Http.Queries;

/// <summary>
/// Represents a query string field that writes a key, an equals sign, and a string or
/// <see cref="ISpanFormattable"/> value, as <c>key=value</c>.
/// </summary>
/// <remarks>
/// <para>
///     The key and a string value are written as given, with no URL escaping, so a caller escapes
///     them first when they hold characters that a query string reserves.
/// </para>
/// <para>
///     A formattable value is formatted each time the field is, so a mutable value that changes
///     after the field is created changes the text that the field writes.
/// </para>
/// <para>
///     A default instance has no key, so <see cref="IsDefaultOrEmpty"/> is <see langword="true"/>
///     and <see cref="TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)"/> writes
///     nothing and returns <see langword="false"/>.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct FormattableQueryField : IQueryField
{
	private readonly string? _key;
	private readonly State _value;

	/// <summary>
	/// Gets the key that the field writes.
	/// </summary>
	/// <value>The field's key, or an empty string for a default instance.</value>
	public string Key => _key ?? string.Empty;
	/// <summary>
	/// Gets a value that indicates whether the field is a default instance with no key.
	/// </summary>
	/// <value>
	///     <see langword="true"/> if the field has no key; otherwise, <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(false, nameof(_key))]
	public bool IsDefaultOrEmpty => _key is null;
	/// <summary>
	/// Gets the largest number of characters that the field writes when formatted.
	/// </summary>
	/// <value>
	///     The length of the key, plus one for the equals sign and the value's maximum length; or 0
	///     for a default instance.
	/// </value>
	public readonly int MaxLength => _value.MaxLength;

	/// <summary>
	/// Initializes a new <see cref="FormattableQueryField"/> instance with the specified key and
	/// string value.
	/// </summary>
	/// <param name="key">The key that the field writes.</param>
	/// <param name="value">
	///     The value that the field writes after the equals sign. A <see langword="null"/> value is
	///     written as an empty string, as <c>key=</c>.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters.</exception>
	public FormattableQueryField(string key, string? value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		value ??= string.Empty;
		_key = key;
		_value = new(value, value.Length + key.Length + 1);
	}
	/// <summary>
	/// Initializes a new <see cref="FormattableQueryField"/> instance with the specified key, a
	/// formattable value, the value's maximum formatted length, and an optional format.
	/// </summary>
	/// <param name="key">The key that the field writes.</param>
	/// <param name="value">The value that the field formats after the equals sign.</param>
	/// <param name="maxLength">The largest number of characters that formatting <paramref name="value"/> produces.</param>
	/// <param name="format">
	///     The format that <paramref name="value"/> is always formatted with, or
	///     <see langword="null"/>, empty, or white space to use the format given when the field is
	///     formatted.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters.</exception>
	public FormattableQueryField(string key, ISpanFormattable value, int maxLength, string? format = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		_key = key;
		FormattableObject obj = new(value, format);
		_value = new(obj, maxLength + key.Length + 1);
	}

	//BUG: Throws instead of returning false when the destination is too short for the key, the equals sign, or a string value.
	/// <summary>
	/// Tries to write the field as <c>key=value</c> into the specified span.
	/// </summary>
	/// <param name="destination">The span to write the field into.</param>
	/// <param name="charsWritten">When this method returns, the number of characters written to <paramref name="destination"/>.</param>
	/// <param name="format">
	///     The format for a formattable value, which the format given to the constructor replaces
	///     when that one isn't <see langword="null"/>, empty, or white space. A string value ignores it.
	/// </param>
	/// <param name="provider">
	///     The culture-specific formatting information for a formattable value, or
	///     <see langword="null"/> for the current culture's. A string value ignores it.
	/// </param>
	/// <returns>
	///     <see langword="true"/> if the whole field fits in <paramref name="destination"/>; otherwise,
	///     or for a default instance, <see langword="false"/>.
	/// </returns>
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		if (this.IsDefaultOrEmpty)
		{
			charsWritten = 0;
			return false;
		}

		_key.AsSpan().CopyTo(destination, out charsWritten);
		destination[charsWritten++] = '=';

		if (_value.TryWrite(destination.Slice(charsWritten), out int written, format, provider))
		{
			charsWritten += written;
			return true;
		}

		return false;
	}

	//BUG: Stack-allocates MaxLength characters with no upper bound, so a long string value can overflow the stack.
	/// <summary>
	/// Returns the field formatted as <c>key=value</c>.
	/// </summary>
	/// <param name="format">
	///     The format for a formattable value, which the format given to the constructor replaces
	///     when that one isn't <see langword="null"/>, empty, or white space. A string value ignores it.
	/// </param>
	/// <param name="formatProvider">
	///     The culture-specific formatting information for a formattable value, or
	///     <see langword="null"/> for the current culture's. A string value ignores it.
	/// </param>
	/// <returns>The formatted field, or an empty string for a default instance or a field that can't be formatted.</returns>
	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		Span<char> chars = stackalloc char[_value.MaxLength];
		if (this.TryFormat(chars, out int charsWritten, format.AsSpan(), formatProvider))
		{
			return new string(chars.Slice(0, charsWritten));
		}
		else
		{
			return string.Empty;
		}
	}

	/// <summary>
	/// Represents a field's value, either a string or a boxed <see cref="FormattableObject"/>, with
	/// the field's maximum length.
	/// </summary>
	/// <remarks>
	/// <see cref="Index"/> records which kind of value the instance holds, so that
	/// <see cref="TryWrite"/> can reinterpret the stored object without a type check. A default
	/// instance holds neither, and its <see cref="Index"/> is 0.
	/// </remarks>
	[StructLayout(LayoutKind.Sequential)]
	private readonly struct State
	{
		private readonly object? _value;
		private readonly uint _index;
		private readonly int _maxLength;

		/// <summary>
		/// Gets the kind of value that the instance holds.
		/// </summary>
		/// <value>1 for a string, 2 for a <see cref="FormattableObject"/>, or 0 for a default instance.</value>
		internal uint Index => _index;
		/// <summary>
		/// Gets the largest number of characters that the owning field writes, its key and equals sign included.
		/// </summary>
		/// <value>The owning field's maximum length.</value>
		internal int MaxLength => _maxLength;

		/// <summary>
		/// Initializes a new <see cref="State"/> instance that holds the specified string value.
		/// </summary>
		/// <param name="value">The string value.</param>
		/// <param name="maxLength">The owning field's maximum length.</param>
		internal State(string value, int maxLength)
		{
			_value = value;
			_maxLength = maxLength;
			_index = 1;
		}
		/// <summary>
		/// Initializes a new <see cref="State"/> instance that holds the specified formattable value,
		/// which it boxes.
		/// </summary>
		/// <param name="value">The formattable value.</param>
		/// <param name="maxLength">The owning field's maximum length.</param>
		internal State(FormattableObject value, int maxLength)
		{
			_value = value;
			_maxLength = maxLength;
			_index = 2;
		}

		/// <summary>
		/// Tries to write the value into the specified span.
		/// </summary>
		/// <param name="destination">The span to write the value into.</param>
		/// <param name="written">When this method returns, the number of characters written, or -1 when nothing was written.</param>
		/// <param name="format">The format for a formattable value.</param>
		/// <param name="formatProvider">The culture-specific formatting information for a formattable value.</param>
		/// <returns>
		///     <see langword="true"/> if the value was written to <paramref name="destination"/>;
		///     otherwise, or for a default instance, <see langword="false"/>.
		/// </returns>
		internal unsafe bool TryWrite(Span<char> destination, out int written, ReadOnlySpan<char> format, IFormatProvider? formatProvider)
		{
			written = _index switch
			{
				1 => WriteString(Unsafe.As<string>(_value!), destination),
				2 => WriteFormattable(ref Unsafe.Unbox<FormattableObject>(_value!), destination, format, formatProvider),
				_ => -1,
			};

			return written >= 0;
		}

		/// <summary>
		/// Copies the specified string into the specified span.
		/// </summary>
		/// <param name="value">The string to copy.</param>
		/// <param name="destination">The span to copy the string into.</param>
		/// <returns>The length of <paramref name="value"/>.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is shorter than <paramref name="value"/>.</exception>
		private static int WriteString(
			string value,
			Span<char> destination)
		{
			value.CopyTo(destination);
			return value.Length;
		}
		/// <summary>
		/// Formats the specified value into the specified span.
		/// </summary>
		/// <param name="formattable">The value to format, read in place from its box.</param>
		/// <param name="destination">The span to format the value into.</param>
		/// <param name="format">The format to use unless the value holds a format of its own.</param>
		/// <param name="formatProvider">The culture-specific formatting information.</param>
		/// <returns>
		///     The number of characters written to <paramref name="destination"/>, or -1 when the
		///     formatted value doesn't fit.
		/// </returns>
		private static int WriteFormattable(
			ref readonly FormattableObject formattable,
			Span<char> destination,
			ReadOnlySpan<char> format,
			IFormatProvider? formatProvider)
		{
			_ = formattable.TryFormat(destination, out int written, format, formatProvider);
			return written;
		}
	}

	/// <summary>
	/// Represents an <see cref="ISpanFormattable"/> value together with the format that it is
	/// always formatted with.
	/// </summary>
	[StructLayout(LayoutKind.Auto)]
	private readonly struct FormattableObject : ISpanFormattable
	{
		/// <summary>
		/// Gets the format that replaces the one given when the value is formatted.
		/// </summary>
		/// <value>
		///     The fixed format, or <see langword="null"/>, empty, or white space to use the format
		///     given when the value is formatted.
		/// </value>
		internal string? Format { get; }
		/// <summary>
		/// Gets the value to format.
		/// </summary>
		/// <value>The wrapped value.</value>
		internal ISpanFormattable Value { get; }

		/// <summary>
		/// Initializes a new <see cref="FormattableObject"/> instance with the specified value and
		/// fixed format.
		/// </summary>
		/// <param name="value">The value to format.</param>
		/// <param name="format">The format that replaces the one given when the value is formatted, or <see langword="null"/>.</param>
		internal FormattableObject(ISpanFormattable value, string? format)
		{
			this.Value = value;
			this.Format = format;
		}

		/// <summary>
		/// Tries to format the value into the specified span.
		/// </summary>
		/// <param name="destination">The span to format the value into.</param>
		/// <param name="charsWritten">When this method returns, the number of characters written to <paramref name="destination"/>.</param>
		/// <param name="format">The format to use when <see cref="Format"/> is <see langword="null"/>, empty, or white space.</param>
		/// <param name="formatProvider">The culture-specific formatting information, or <see langword="null"/> for the current culture's.</param>
		/// <returns>
		///     <see langword="true"/> if the formatted value fits in <paramref name="destination"/>;
		///     otherwise, <see langword="false"/>.
		/// </returns>
		public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? formatProvider)
		{
			if (!string.IsNullOrWhiteSpace(this.Format))
			{
				format = this.Format;
			}

			return this.Value.TryFormat(destination, out charsWritten, format, formatProvider);
		}
		//BUG: Formats with the format given rather than Format, so it disagrees with TryFormat when Format is set.
		/// <summary>
		/// Returns the value formatted with the specified format.
		/// </summary>
		/// <param name="format">The format to use when <see cref="Format"/> is <see langword="null"/>, empty, or white space.</param>
		/// <param name="formatProvider">The culture-specific formatting information, or <see langword="null"/> for the current culture's.</param>
		/// <returns>The formatted value.</returns>
		public string ToString(string? format, IFormatProvider? formatProvider)
		{
			return this.Value.ToString(format, formatProvider);
		}
	}
}
