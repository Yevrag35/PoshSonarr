using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Strings;

namespace MG.Sonarr.Next.Strings;

public readonly partial struct Wildcard
{

	/// <summary>
	/// Converts the current <see cref="Wildcard"/> instance into a pattern where input can contain the value.
	/// </summary>
	/// <returns>
	/// A new <see cref="Wildcard"/> instance with a <see cref="MatchType"/> equaling <see cref="WildcardMatchType.Like"/>.
	/// <para>
	/// If the current instance is empty, then returns an empty instance. If the current instance has
	/// the match type <see cref="WildcardMatchType.All"/> or is already a "like" pattern, then returns the current instance.
	/// </para>
	/// </returns>
	public Wildcard ToContainsLike()
	{
		if (this.IsEmpty)
		{
			return Empty;
		}

		if (this.MatchType == WildcardMatchType.All)
		{
			return All;
		}

		ReadOnlySpan<char> pattern = _pattern;
		if (this.MatchType == WildcardMatchType.Like && pattern.EnclosedIn('*', '*'))
		{
			return this;
		}

		int length = pattern.Length + 2;
		using (var buffer = RentedBuffer.Rent<char>(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length))
		{
			int pos = 0;
			if (pattern[0] != '*')
			{
				buffer[pos++] = '*';
			}

			pos = pattern.CopyToSlice(buffer.Span, pos);
			if (pattern[^1] != '*')
			{
				buffer[pos++] = '*';
			}

			return new(buffer[..pos], WildcardMatchType.Like);
		}
	}

	/// <summary>
	/// Converts the current <see cref="Wildcard"/> instance to an exact pattern.
	/// </summary>
	/// <remarks>
	/// This method removes all wildcard characters from the resulting pattern.
	/// </remarks>
	/// <returns>
	/// A new <see cref="Wildcard"/> instance with all wildcard characters removed and a <see cref="MatchType"/>
	/// equaling <see cref="WildcardMatchType.Exact"/>.
	/// <para>
	/// If the current instance is empty or <see cref="WildcardMatchType.All"/>, returns an empty instance.
	/// If the current instance is already an exact pattern, returns the current instance unchanged.
	/// </para>
	/// </returns>
	public Wildcard ToExact()
	{
		if (this.MatchType == WildcardMatchType.Exact)
		{
			return this;
		}

		if (this.IsEmpty || this.MatchType == WildcardMatchType.All)
		{
			return Empty;
		}

		ReadOnlySpan<char> pattern = _pattern;
		int length = _state.Length;

		using (var buffer = RentedBuffer.Rent<char>(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length))
		{
			int written = pattern.RemoveAny(buffer.Span, _wildcardChars);
			return new(buffer[..written], WildcardMatchType.Exact);
		}
	}
}
