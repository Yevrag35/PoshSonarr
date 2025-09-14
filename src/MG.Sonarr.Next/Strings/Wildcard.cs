using System.Buffers;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Strings;

/// <summary>
/// A read-only <see cref="string"/> that can be used for checking case-insensitive equality and pattern matching based on traditional wildcard characters.
/// </summary>
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{GetDebuggerString(),nq}")]
public readonly partial struct Wildcard :
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
	/// <returns>The char object at the specified index.</returns>
	/// <exception cref="ArgumentOutOfRangeException"/>
	public readonly ref readonly char this[int index] => ref _pattern.AsSpan()[index];

	/// <summary>
	/// Indicates whether the <see cref="Wildcard"/> contains any wildcard characters ('?' or '*') in
	/// the string.
	/// </summary>
	/// <remarks>
	///     If <see langword="false"/>, during the <see cref="IsMatch(ReadOnlySpan{char})"/> and
	///     <see cref="IsMatch(string?)"/> method executions, only strict <see cref="char"/> equality will
	///     be checked.
	/// </remarks>
	/// <returns>
	///     <see langword="true"/> if the string contains at least 1 wildcard character; otherwise,
	///     <see langword="false"/>.
	/// </returns>
	[MemberNotNullWhen(true, nameof(_pattern))]
	public readonly bool ContainsWildcards
	{
		get => !this.IsEmpty && _matchType != WildcardMatchType.Exact;
	}
	/// <summary>
	/// Indicates whether this <see cref="Wildcard"/> object is empty or default-initialized.
	/// </summary>
	/// <remarks>
	/// An empty <see cref="Wildcard"/> instance will never match any input.
	/// </remarks>
	[MemberNotNullWhen(false, nameof(_pattern))]
	public readonly bool IsEmpty => _length == 0;
	/// <summary>
	/// Gets the number of characters in the current <see cref="Wildcard"/> pattern.
	/// </summary>
	public readonly int Length => _length;
	/// <summary>
	/// Gets the match type that this <see cref="Wildcard"/> instance will follow when comparing its pattern against input.
	/// </summary>
	public readonly WildcardMatchType MatchType => _matchType;

	/// <summary>
	/// Initializes a new instance of the <see cref="Wildcard"/> struct using the 
	/// specified <see cref="string"/>.
	/// </summary>
	/// <remarks>
	/// If <paramref name="patternString"/> is <see langword="null"/> or an empty string, the constructed <see cref="Wildcard"/> instance
	/// will be treated as "empty" and will only match other empty spans or <see cref="string"/> instances.
	/// Functionality equivalent to <see cref="Empty"/>.
	/// </remarks>
	/// <param name="pattern">The string to use as the wildcard pattern.</param>
	[DebuggerStepThrough]
	public Wildcard(string? patternString)
		: this(pattern: patternString.AsSpan())
	{
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="Wildcard"/> struct using the 
	/// specified read-only span of <see cref="char"/> elements.
	/// </summary>
	/// <remarks>
	/// If <paramref name="pattern"/> is an empty <see cref="ReadOnlySpan{T}"/>, the constructed <see cref="Wildcard"/> instance
	/// will be treated as "empty" and will only match other empty spans or <see cref="string"/> instances.
	/// Functionality equivalent to <see cref="Empty"/>.
	/// </remarks>
	/// <param name="pattern">The read-only character span to use as the wildcard pattern.</param>
	[DebuggerStepThrough]
	public Wildcard(ReadOnlySpan<char> pattern)
	{
		WildcardMatchType matchType = DeterminePattern(pattern.Trim());
		_pattern = ConstructPattern(pattern, in matchType, ref _length, ref _isNotEmpty);
		_matchType = matchType;
	}
	private Wildcard(ReadOnlySpan<char> pattern, WildcardMatchType matchType)
	{
		_isNotEmpty = !pattern.IsEmpty;
		_pattern = pattern.ToString();
		_length = pattern.Length;
		_matchType = matchType;
	}
	private Wildcard(bool isAll)
	{
		if (isAll)
		{
			_matchType = WildcardMatchType.All;
			_length = 1;
			_pattern = ALL_STRING;
			_isNotEmpty = true;
		}
		else
		{
			_matchType = WildcardMatchType.None;
			_length = 0;
			_pattern = string.Empty;
			_isNotEmpty = false;
		}
	}

	#region PRIVATE FIELDS

	const StringComparison DEFAULT_COMPARISON = StringComparison.OrdinalIgnoreCase;
	const int MAX_STACKALLOC = 256;
	private const string ALL_STRING = "*";
	static readonly SearchValues<char> _wildcardChars = SearchValues.Create(['*', '?']);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly WildcardMatchType _matchType;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly int _length;
	private readonly string? _pattern;

	#endregion

	[DebuggerStepThrough]
	[ExcludeFromCodeCoverage]
	[EditorBrowsable(EditorBrowsableState.Never)]
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