using System.ComponentModel;

namespace MG.Sonarr.Next.Strings;

/// <summary>
/// Represents a read-only wildcard pattern used for case-insensitive equality and wildcard matching.
/// </summary>
/// <remarks>
///     <para>
///         <c>*</c> and <c>?</c> are the only wildcard characters. Unlike
///         <see cref="System.Management.Automation.WildcardPattern"/>, which reads <c>[abc]</c> as a set of characters
///         and a backtick as an escape, and throws for a <c>[</c> that nothing closes, a pattern matches
///         <c>[</c>, <c>]</c>, and the backtick as themselves, so a name that holds them needs no escaping.
///     </para>
///     <para>
///         <see cref="IsMatch(string?)"/> and <see cref="IsMatch(ReadOnlySpan{char})"/> compare with
///         <see cref="StringComparison.OrdinalIgnoreCase"/>, the same in every culture, where
///         <see cref="System.Management.Automation.WildcardPattern"/> with
///         <see cref="System.Management.Automation.WildcardOptions.IgnoreCase"/> folds case with the current culture. So
///         <c>i</c> matches <c>I</c> under tr-TR too, and the Kelvin sign, U+212A, matches neither <c>K</c> nor
///         <c>k</c>. A surrogate pair in the text folds as one character, as
///         <see cref="StringComparison.OrdinalIgnoreCase"/> folds it, where the matcher of
///         <see cref="System.Management.Automation.WildcardPattern"/> folds each half on its own.
///     </para>
///     <para>
///         An empty pattern, such as <see cref="Empty"/>, matches nothing, not even empty text, which an empty
///         <see cref="System.Management.Automation.WildcardPattern"/> matches.
///     </para>
/// </remarks>
[DebuggerDisplay("{GetDebuggerString(),nq}")]
public readonly partial struct Wildcard : // 16 bytes
	IEnumerable<char>,
	IEquatable<string>, IEquatable<Wildcard>,
	ISpanFormattable, ISpanParsable<Wildcard>,
	IUtf8SpanFormattable, IUtf8SpanParsable<Wildcard>
{
	// -- Other partial file locations:
	//
	// Formatting methods -> ./Wildcard-Formatting.cs
	// Equality methods/Equality operators/Explicit and Implicit operator -> ./Wildcard-EqualityAndOperators.cs
	// Main Methods -> ./Wildcard-Methods.cs
	// Parsing methods -> ./Wildcard-Parsing.cs
	// --------------------------------

	// -- PRIVATE FIELDS

	private const StringComparison DEFAULT_COMPARISON = StringComparison.OrdinalIgnoreCase;
	private const int MAX_STACKALLOC = 256;
	private const string ALL_STRING = "*";
	private static readonly SearchValues<char> s_wildcardChars = SearchValues.Create(['*', '?']);

	private readonly string? _pattern; // 8 bytes
	private readonly State _state;     // 8 bytes

	// --

	/// <summary>
	/// A <see langword="static"/>, read-only instance of <see cref="Wildcard"/> representing a pattern
	/// that matches any and all input.
	/// </summary>
	public static readonly Wildcard All = new(isAll: true);
	/// <summary>
	/// A <see langword="static"/>, read-only instance of <see cref="Wildcard"/> representing an 
	/// empty pattern that never matches any input.
	/// </summary>
	public static readonly Wildcard Empty = new(isAll: false);

	/// <summary>
	/// Gets the <see cref="char"/> object at the specified position in the current <see cref="Wildcard"/>
	/// object.
	/// </summary>
	/// <param name="index">The position in the current string.</param>
	/// <exception cref="IndexOutOfRangeException">
	/// Thrown when <paramref name="index"/> is less than zero or not less than <see cref="Length"/>, as
	/// <see cref="string"/>'s indexer throws.
	/// </exception>
	/// <value>The character at the specified index.</value>
	/// <exception cref="NullReferenceException">The underlying pattern is <see langword="null"/>.</exception>
	public char this[int index] => _pattern![index];

	/// <summary>
	/// Indicates whether the <see cref="Wildcard"/> contains any wildcard characters ('?' or '*') in
	/// the string.
	/// </summary>
	/// <remarks>
	///     If <see langword="false"/>, the <see cref="IsMatch(ReadOnlySpan{char})"/> and
	///     <see cref="IsMatch(string?)"/> methods compare the whole input with the pattern, as
	///     <see cref="Equals(ReadOnlySpan{char})"/> does.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if the string contains at least one wildcard character; otherwise,
	/// <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(_pattern))]
	public bool ContainsWildcards
	{
		[DebuggerStepThrough]
		get => !this.IsEmpty && _state.Type != WildcardMatchType.Exact;
	}
	/// <summary>
	/// Indicates whether this <see cref="Wildcard"/> object is empty or default-initialized.
	/// </summary>
	/// <remarks>
	/// An empty <see cref="Wildcard"/> instance will never match any input.
	/// </remarks>
	/// <value><see langword="true"/> when the instance is empty; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(false, nameof(_pattern))]
	public bool IsEmpty => _state.Length == 0;
	/// <summary>
	/// Gets the number of characters in the current <see cref="Wildcard"/> pattern.
	/// </summary>
	/// <value>The number of characters in the wildcard pattern.</value>
	public int Length => _state.Length;
	/// <summary>
	/// Gets the match type that this <see cref="Wildcard"/> instance will follow when comparing its pattern against input.
	/// </summary>
	/// <value>The pattern match type.</value>
	public WildcardMatchType MatchType => _state.Type;

	/// <summary>
	/// Initializes a new instance of the <see cref="Wildcard"/> struct using the 
	/// specified <see cref="string"/>.
	/// </summary>
	/// <remarks>
	/// If <paramref name="patternString"/> is <see langword="null"/> or an empty string, the constructed <see cref="Wildcard"/> instance
	/// will be treated as "empty": it never matches any input, and it equals only other empty spans, <see cref="string"/>
	/// instances, and <see cref="Wildcard"/> instances. Functionality equivalent to <see cref="Empty"/>.
	/// </remarks>
	/// <param name="patternString">The string to use as the wildcard pattern.</param>
	public Wildcard(string? patternString)
	{
		if (string.IsNullOrEmpty(patternString))
		{
			this = Empty;
			return;
		}

		WildcardMatchType matchType = DeterminePattern(patternString); // The containing null or empty check will be elided because of the check above.
		Debug.Assert(matchType != WildcardMatchType.None, "DeterminePattern should not return None for a non-empty string.");
		int length = 0;
		_pattern = ConstructPattern(patternString, matchType, ref length);
		_state = new(length: length, type: matchType);
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="Wildcard"/> struct using the 
	/// specified read-only span of <see cref="char"/> elements.
	/// </summary>
	/// <remarks>
	/// If <paramref name="pattern"/> is an empty <see cref="ReadOnlySpan{T}"/>, the constructed <see cref="Wildcard"/> instance
	/// will be treated as "empty": it never matches any input, and it equals only other empty spans, <see cref="string"/>
	/// instances, and <see cref="Wildcard"/> instances. Functionality equivalent to <see cref="Empty"/>.
	/// </remarks>
	/// <param name="pattern">The read-only character span to use as the wildcard pattern.</param>
	public Wildcard(ReadOnlySpan<char> pattern)
	{
		WildcardMatchType matchType = DeterminePattern(pattern);
		int length = 0;
		_pattern = ConstructPattern(pattern, matchType, ref length);
		_state = new(length: length, type: matchType);
	}

	/// <summary>
	/// Initializes a new <see cref="Wildcard"/> instance with the specified pattern and match type.
	/// </summary>
	/// <param name="pattern">The pattern text.</param>
	/// <param name="matchType">The match type to assign to the instance.</param>
	private Wildcard(ReadOnlySpan<char> pattern, WildcardMatchType matchType)
	{
		_pattern = pattern.ToString();
		_state = new(length: _pattern.Length, type: matchType);
	}

	/// <summary>
	/// Initializes a new <see cref="Wildcard"/> instance as either the all-matching value or the empty value.
	/// </summary>
	/// <param name="isAll"><see langword="true"/> to create the all-matching value; otherwise, <see langword="false"/>.</param>
	[DebuggerStepThrough]
	private Wildcard(bool isAll)
	{
		if (isAll)
		{
			_state = new(length: 1, type: WildcardMatchType.All);
			_pattern = ALL_STRING;
		}
		else
		{
			_state = default;
			_pattern = string.Empty;
		}
	}

	/// <summary>
	/// Gets a debugger display string for the current instance.
	/// </summary>
	/// <returns>A formatted debugger display string.</returns>
	[ExcludeFromCodeCoverage]
	[DebuggerStepThrough, EditorBrowsable(EditorBrowsableState.Never)]
	private string GetDebuggerString()
	{
		if (this.IsEmpty)
		{
			return "\\{ MatchType = None \\}";
		}

		if (this.MatchType == WildcardMatchType.Exact)
		{
			return $@"{{ MatchType = {this.MatchType}, Value = ""{_pattern.Replace("\"", "\\\"", StringComparison.Ordinal)}"" }}";
		}
		else
		{
			return $@"{{ MatchType = {this.MatchType}, Value = {_pattern} }}";
		}
	}
}

/// <summary>
/// Defines the type of matching a <see cref="Wildcard"/> instance will use when comparing the pattern and input.
/// </summary>
public enum WildcardMatchType
{
	/// <summary>
	/// Indicates that the pattern is empty and that no input will ever match.
	/// </summary>
	None = 0,
	/// <summary>
	/// Indicates that the input must contain all or portions of the pattern.
	/// </summary>
	Like = 1,
	/// <summary>
	/// Indicates that the input must start with the pattern.
	/// </summary>
	StartsWith = 2,
	/// <summary>
	/// Indicates that the input must end with the pattern.
	/// </summary>
	EndsWith = 3,
	/// <summary>
	/// Indicates that the input must match the pattern exactly.
	/// </summary>
	Exact = 4,
	/// <summary>
	/// Indicates that the pattern is a singular <c>*</c> character and will match any input, including empty input.
	/// </summary>
	All = 10,
}