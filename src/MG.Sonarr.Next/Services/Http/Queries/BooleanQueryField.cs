using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Services.Http.Queries;

/// <summary>
/// Represents a query string field that writes a key and a Boolean value as <c>key=true</c> or
/// <c>key=false</c>.
/// </summary>
/// <remarks>
/// <para>
///     A collection expression of a key and a value, such as <c>[key, value]</c>, creates a field
///     through <see cref="CreateFromSpan(ReadOnlySpan{Either{string, bool}})"/>, and enumerating a
///     field yields its key and then its value.
/// </para>
/// <para>
///     A default instance has no key, so <see cref="IsDefaultOrEmpty"/> is <see langword="true"/>
///     and <see cref="TryFormat(Span{char}, out int)"/> writes nothing and returns
///     <see langword="false"/>. The key is written as given, with no URL escaping.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
[CollectionBuilder(typeof(BooleanQueryField), nameof(CreateFromSpan))]
public readonly struct BooleanQueryField : IQueryField, IEnumerable<Either<string, bool>>
{
	private const string TRUE = "true", FALSE = "false";

	private readonly string? _key;
	private readonly bool _value;
	private readonly bool _isNotEmpty;
	private readonly int _maxLength;

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
	public bool IsDefaultOrEmpty => !_isNotEmpty;
	/// <summary>
	/// Gets the largest number of characters that the field writes when formatted.
	/// </summary>
	/// <value>
	///     The length of the key, plus one for the equals sign and five for <c>false</c>, the longer
	///     of the two values; or 0 for a default instance.
	/// </value>
	public readonly int MaxLength => _maxLength;

	/// <summary>
	/// Initializes a new <see cref="BooleanQueryField"/> instance with the specified key and value.
	/// </summary>
	/// <param name="key">The key that the field writes.</param>
	/// <param name="value">The Boolean value that the field writes after the key.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters.</exception>
	public BooleanQueryField(string key, bool value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		_key = key;
		_value = value;
		_maxLength = key.Length + 1 + FALSE.Length;
		_isNotEmpty = true;
	}

	/// <summary>
	/// Returns an enumerator that yields the field's key and then its value.
	/// </summary>
	/// <returns>An enumerator over the key, as the first case, and the value, as the second.</returns>
	[DebuggerStepThrough]
	IEnumerator<Either<string, bool>> IEnumerable<Either<string, bool>>.GetEnumerator()
	{
		yield return Either<string, bool>.FromT1(this.Key);
		yield return Either<string, bool>.FromT2(_value);
	}
	/// <summary>
	/// Returns an enumerator that yields the field's key and then its value.
	/// </summary>
	/// <returns>An enumerator over the key and the value.</returns>
	[DebuggerStepThrough]
	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<Either<string, bool>>)this).GetEnumerator();
	}

	/// <summary>
	/// Creates a field with the specified key whose value is <see langword="true"/>.
	/// </summary>
	/// <param name="key">The key that the field writes.</param>
	/// <returns>A field that writes <c>key=true</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or consists only of white-space characters.</exception>
	public static BooleanQueryField CreateTrue(string key)
	{
		return new BooleanQueryField(key, value: true);
	}
	/// <summary>
	/// Creates a field from a key and a Boolean value, given in either order.
	/// </summary>
	/// <remarks>
	/// The compiler calls this method for a collection expression that targets
	/// <see cref="BooleanQueryField"/>, such as <c>[key, value]</c>.
	/// </remarks>
	/// <param name="values">Exactly two values: one key and one Boolean value.</param>
	/// <returns>
	///     A field with the key and value from <paramref name="values"/>; or a default instance when
	///     <paramref name="values"/> doesn't hold exactly two values or holds two of the same kind.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when the key in <paramref name="values"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when the key in <paramref name="values"/> is empty or consists only of white-space characters.</exception>
	public static BooleanQueryField CreateFromSpan(params ReadOnlySpan<Either<string, bool>> values)
	{
		if (values.Length != 2)
		{
			return default;
		}

		ref readonly var first = ref values[0];
		ref readonly var second = ref values[1];
		if (first.IsSameType(in second))
		{
			return default;
		}

		return first.Match(second,
			static (key, other) => new BooleanQueryField(key, other.AsT2),
			static (boolean, other) => new BooleanQueryField(other.AsT1!, boolean));
	}

	/// <summary>
	/// Returns the field formatted as <c>key=true</c> or <c>key=false</c>.
	/// </summary>
	/// <param name="format">Ignored; the field has one format.</param>
	/// <param name="formatProvider">Ignored; the value is always written in lowercase English.</param>
	/// <returns>The formatted field, or an empty string for a default instance.</returns>
	string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
	{
		Span<char> chars = stackalloc char[this.MaxLength];
		if (!this.TryFormat(chars, out int charsWritten))
		{
			return string.Empty;
		}

		return new string(chars.Slice(0, charsWritten));
	}
	/// <summary>
	/// Tries to write the field as <c>key=true</c> or <c>key=false</c> into the specified span.
	/// </summary>
	/// <param name="destination">The span to write the field into.</param>
	/// <param name="charsWritten">When this method returns, the number of characters written to <paramref name="destination"/>.</param>
	/// <returns>
	///     <see langword="true"/> if the whole field fits in <paramref name="destination"/>; otherwise,
	///     or for a default instance, <see langword="false"/>.
	/// </returns>
	public bool TryFormat(Span<char> destination, out int charsWritten)
	{
		charsWritten = 0;
		if (this.IsDefaultOrEmpty)
		{
			return false;
		}

		if (!_key.AsSpan().TryCopyToSlice(destination, ref charsWritten))
		{
			return false;
		}

		destination[charsWritten++] = '=';

		string value = _value ? TRUE : FALSE;
		return value.AsSpan().TryCopyToSlice(destination, ref charsWritten);
	}
	/// <summary>
	/// Tries to write the field as <c>key=true</c> or <c>key=false</c> into the specified span.
	/// </summary>
	/// <param name="destination">The span to write the field into.</param>
	/// <param name="charsWritten">When this method returns, the number of characters written to <paramref name="destination"/>.</param>
	/// <param name="format">Ignored; the field has one format.</param>
	/// <param name="provider">Ignored; the value is always written in lowercase English.</param>
	/// <returns>
	///     <see langword="true"/> if the whole field fits in <paramref name="destination"/>; otherwise,
	///     or for a default instance, <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		return this.TryFormat(destination, out charsWritten);
	}
}
