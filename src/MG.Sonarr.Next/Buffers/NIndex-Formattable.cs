namespace MG.Sonarr.Next.Buffers;

public readonly partial struct NIndex
{
	/// <summary>
	/// Converts the numerical value of the index to its equivalent string representation.
	/// </summary>
	/// <returns>
	/// The string representation of the value of this instance, consisting of a
	/// negative sign if this index is invalid (negative), and a sequence of digits ranging from 0 to 9 
	/// with no leading zeroes.
	/// </returns>
	[DebuggerStepThrough]
	public override string ToString()
	{
		return _data.ToString();
	}

	/// <inheritdoc/>
	[DebuggerStepThrough]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
	{
		return _data.TryFormat(destination, out charsWritten, format, provider);
	}
	[DebuggerStepThrough]
	/// <inheritdoc/>
	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
	{
		return _data.TryFormat(utf8Destination, out bytesWritten, format, provider);
	}

	/// <inheritdoc/>
	[DebuggerStepThrough]
	string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
	{
		return _data.ToString(format, formatProvider);
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		return this.TryFormat(destination, out charsWritten, format, provider);
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		return this.TryFormat(utf8Destination, out bytesWritten, format, provider);
	}
}