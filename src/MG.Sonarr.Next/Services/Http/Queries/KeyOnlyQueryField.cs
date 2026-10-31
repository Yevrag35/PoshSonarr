namespace MG.Sonarr.Next.Services.Http.Queries;

/// <summary>
/// Represents a query string field that consists of a key alone, with no equals sign or value.
/// </summary>
/// <remarks>
/// <para>
///     Two instances are equal when their keys are equal by ordinal, case-sensitive comparison.
///     A default instance has no key, so <see cref="IsDefaultOrEmpty"/> is <see langword="true"/>
///     and the field formats as an empty string.
/// </para>
/// <para>The key is written as given, with no URL escaping.</para>
/// </remarks>
[DebuggerStepThrough]
[StructLayout(LayoutKind.Auto)]
public readonly record struct KeyOnlyQueryField : IQueryField
{
	private readonly string? _key;

	/// <summary>
	/// Gets a value that indicates whether the field is a default instance with no key.
	/// </summary>
	/// <value>
	///     <see langword="true"/> if the field has no key; otherwise, <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(false, nameof(_key))]
	public bool IsDefaultOrEmpty => _key is null;
	/// <summary>
	/// Gets the key that the field writes.
	/// </summary>
	/// <value>The field's key, or an empty string for a default instance.</value>
	public string Key => _key ?? string.Empty;
	/// <summary>
	/// Gets the largest number of characters that the field writes when formatted.
	/// </summary>
	/// <value>The length of <see cref="Key"/>.</value>
	public int MaxLength => this.Key.Length;

	/// <summary>
	/// Initializes a new <see cref="KeyOnlyQueryField"/> instance with the specified key.
	/// </summary>
	/// <param name="key">The key that the field writes.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters.</exception>
	public KeyOnlyQueryField(string key)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		_key = key;
	}

	/// <summary>
	/// Returns the field's key.
	/// </summary>
	/// <param name="format">Ignored; the key is written as is.</param>
	/// <param name="formatProvider">Ignored; the key is written as is.</param>
	/// <returns>The value of <see cref="Key"/>.</returns>
	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		return this.Key;
	}

	//BUG: charsWritten is set to the key's length even when the key doesn't fit and nothing is written.
	/// <summary>
	/// Tries to write the field's key into the specified span.
	/// </summary>
	/// <param name="destination">The span to write the key into.</param>
	/// <param name="charsWritten">When this method returns, the number of characters written to <paramref name="destination"/>.</param>
	/// <param name="format">Ignored; the key is written as is.</param>
	/// <param name="provider">Ignored; the key is written as is.</param>
	/// <returns>
	///     <see langword="true"/> if the key fits in <paramref name="destination"/>; otherwise,
	///     <see langword="false"/>.
	/// </returns>
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		bool result = this.Key.AsSpan().TryCopyTo(destination);
		charsWritten = this.Key.Length;
		return result;
	}
}
