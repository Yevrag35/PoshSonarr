using MG.Sonarr.Next.Extensions.Strings;
using System.Text;

namespace MG.Sonarr.Next.Strings;

public readonly partial struct Wildcard
{
	/// <summary>
	/// Attempts to format the current <see cref="Wildcard"/> instance into the provided span of characters.
	/// </summary>
	/// <param name="destination">The span of characters to format the current instance and copy into.</param>
	/// <param name="charsWritten">
	/// When this method returns, contains the number of characters written into <paramref name="destination"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	public readonly bool TryFormat(Span<char> destination, out int charsWritten)
	{
		charsWritten = 0;
		return _pattern.AsSpan().TryCopyToSlice(destination, ref charsWritten);
	}
	/// <summary>
	/// Attempts to format the current <see cref="Wildcard"/> instance's encoded UTF-8 characters into a span of bytes.
	/// </summary>
	/// <param name="utf8Destination">The span of bytes to format the current instance and copy into.</param>
	/// <param name="bytesWritten">
	/// When this method returns, contains the number of bytes written into <paramref name="utf8Destination"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.
	/// </returns>
	public readonly bool TryFormat(Span<byte> utf8Destination, out int bytesWritten)
	{
		if (this.IsEmpty)
		{
			bytesWritten = 0;
			return true;
		}

		return Encoding.UTF8.TryGetBytes(_pattern, utf8Destination, out bytesWritten);
	}

	/// <summary>
	/// Returns the underlying <see cref="string"/> instance of this <see cref="Wildcard"/> object; no
	/// actual conversion is performed.
	/// </summary>
	/// <returns>The underlying <see cref="string"/>.</returns>
	[DebuggerStepThrough]
	public override string ToString()
	{
		return !this.IsEmpty ? _pattern : string.Empty;
	}

	#region INTERFACE IMPLEMENTATIONS
	/// <inheritdoc/>
	[DebuggerStepThrough]
	readonly bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		return this.TryFormat(destination, out charsWritten);
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	readonly bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		return this.TryFormat(utf8Destination, out bytesWritten);
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	readonly string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
	{
		return this.ToString();
	}

	#endregion
}