namespace MG.Sonarr.Next.Extensions.Strings;

/// <summary>
/// Custom extension methods for <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> instances
/// of type <see cref="char"/>.
/// </summary>
public static partial class CharSpanExtensions
{
	/// <summary>
	/// Provides extension methods that examine, search, and copy a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.
	/// </summary>
	/// <param name="chars">The source span of characters.</param>
	extension(ReadOnlySpan<char> chars)
	{
		/// <summary>
		/// Determines whether the specified span is enclosed by the given character.
		/// </summary>
		/// <param name="value">The character that should appear at the start and end of the span.</param>
		/// <returns><see langword="true"/> if the span begins and ends with the specified character; otherwise,
		/// <see langword="false"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool EnclosedIn(char value)
		{
			return EnclosedInCore(chars, value, value);
		}
		/// <summary>
		/// Determines whether the specified span is enclosed by the given opening and closing characters.
		/// </summary>
		/// <param name="openingChar">The character that should appear at the start of the span.</param>
		/// <param name="closingChar">The character that should appear at the end of the span.</param>
		/// <returns><see langword="true"/> if the span begins with the opening character and ends with the closing character; otherwise,
		/// <see langword="false"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool EnclosedIn(char openingChar, char closingChar)
		{
			return EnclosedInCore(chars, openingChar, closingChar);
		}
		/// <summary>
		/// Copies characters from the source span to the destination span, omitting any characters that match those
		/// specified in the <paramref name="removeChars"/> set.
		/// </summary>
		/// <remarks>
		/// <para>The operation does not allocate additional memory and is suitable for performance-critical
		/// scenarios.</para>
		/// <para><paramref name="destination"/> must be at least as long as <paramref name="chars"/>, however many
		/// characters are removed, so that its length is checked once, before anything is copied, rather than on
		/// every copy.</para>
		/// </remarks>
		/// <param name="destination">The span that receives the characters that aren't removed. It must be at least as
		/// long as <paramref name="chars"/>.</param>
		/// <param name="removeChars">A set of characters to remove from the source span during copying. Any character in this set will be
		/// excluded from the destination.</param>
		/// <returns>The number of characters written to the destination span after removal of specified characters.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is shorter than
		/// <paramref name="chars"/>.</exception>
		public int RemoveAny(Span<char> destination, SearchValues<char> removeChars)
		{
			if (destination.Length < chars.Length)
			{
				throwTooShort(chars.Length, destination.Length);
			}

			int written = 0;
			ReadOnlySpan<char> slice = chars;

			// written + slice.Length starts at chars.Length and drops by one with each removed character,
			// so with the check above, every copy below lands inside destination.
			while (!slice.IsEmpty)
			{
				int index = slice.IndexOfAny(removeChars);
				if (index == -1)
				{
					// No more matches, copy the remaining portion and exit.
					slice.UnsafeCopy(destination.UnsafeSlice(written));
					written += slice.Length;
					break;
				}

				// IndexOfAny returned an index in [0, slice.Length), so both slices are in range, and
				// a match at the slice's last character leaves it empty.
				if (index != 0)
				{
					slice.UnsafeSlice(0, index).UnsafeCopy(destination.UnsafeSlice(written));
					written += index;
				}

				slice = slice.UnsafeSlice(index + 1);
			}

			return written;

			[DoesNotReturn] // Adding 'NoInlining' causes the JIT to not remember this is a non-returning method. Without it, it still doesn't inline.
			static void throwTooShort(int sourceLength, int destinationLength)
			{
				throw new ArgumentException(
					string.Format(CultureInfo.CurrentCulture, ExMessages.Buffer_TooSmall, sourceLength, destinationLength),
					nameof(destination));
			}
		}

		/// <summary>
		/// Attemps to copy the contents of this <see cref="ReadOnlySpan{T}"/> into a
		/// <see cref="Span{T}"/> and advancing the given ref <see cref="int"/> the number
		/// of characters that were copied, returning a value indicating whether or not the operation
		/// succeeded.
		/// </summary>
		/// <param name="span">The target of the copy operation.</param>
		/// <param name="position">
		///     The ref <see cref="int"/> to add the number of the characters to if copying was
		///     successful.
		/// </param>
		/// <returns>
		///     <see langword="true"/> if the copying operation was successful; otherwise
		///     <see langword="false"/>.
		/// </returns>
		public bool TryCopyToSlice(Span<char> span, ref int position)
		{
			bool result = false;

			if (chars.TryCopyTo(span.Slice(position)))
			{
				position += chars.Length;
				result = true;
			}

			return result;
		}

		/// <summary>
		/// Determines whether the beginning of the <paramref name="chars"/> matches the specified <paramref name="value"/> when compared ordinally.
		/// </summary>
		/// <param name="value">The character to compare to the beginning of the source span.</param>
		/// <returns>
		///     <see langword="true"/> if <paramref name="value"/> matches the beginning of
		///     <paramref name="chars"/>; otherwise, <see langword="false"/>.
		/// </returns>
		[DebuggerStepThrough]
		public bool StartsWith(char value)
		{
			return FirstCharEquals(chars, value);
		}
		/// <summary>
		/// Determines whether the beginning of the <paramref name="chars"/> matches the specified <paramref name="value"/> when compared using the specified
		/// <paramref name="comparisonType"/> option.
		/// </summary>
		/// <param name="value">The character to compare to the beginning of the source span.</param>
		/// <param name="comparisonType">
		///     One of the enumeration values that determines how the
		///     <paramref name="chars"/> and <paramref name="value"/> are compared.
		/// </param>
		/// <returns>
		///     <see langword="true"/> if <paramref name="value"/> matches the beginning of
		///     <paramref name="chars"/>; otherwise, <see langword="false"/>.
		/// </returns>
		[DebuggerStepThrough]
		public bool StartsWith(char value, StringComparison comparisonType)
		{
			return chars.StartsWith(new ReadOnlySpan<char>(in value), comparisonType);
		}

		/// <summary>
		/// Attempts to find the last occurrence of a specified character within the span.
		/// </summary>
		/// <remarks>This method does not throw an exception if the character is not found. Instead, it returns <see
		/// langword="false"/>  and sets <paramref name="index"/> to -1.</remarks>
		/// <param name="c">The character to locate within the span.</param>
		/// <param name="index">When this method returns, contains the zero-based index of the last occurrence of the specified character,  if
		/// found; otherwise, -1.</param>
		/// <returns><see langword="true"/> if the specified character is found in the span; otherwise, <see langword="false"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryLastIndexOf(char c, out int index)
		{
			index = chars.LastIndexOf(c);
			return index >= 0;
		}
		/// <summary>
		/// Attempts to find the last occurrence of a specified substring within the current span using the specified string
		/// comparison.
		/// </summary>
		/// <param name="value">The substring to locate within the span.</param>
		/// <param name="comparisonType">The type of string comparison to use when searching for the substring.</param>
		/// <param name="index">When this method returns, contains the zero-based index of the last occurrence of <paramref name="value"/> within
		/// <paramref name="chars"/>, if found; otherwise, -1.</param>
		/// <returns><see langword="true"/> if the substring is found within the span; otherwise, <see langword="false"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryLastIndexOf([ConstantExpected] string value, StringComparison comparisonType, out int index)
		{
			index = chars.LastIndexOf(value, comparisonType);
			return index != -1;
		}
	}

	/// <summary>
	/// Provides extension methods that format an <see cref="ISpanFormattable"/> value into a <see cref="Span{T}"/> of
	/// <see cref="char"/>.
	/// </summary>
	/// <typeparam name="T">The type of the value to format.</typeparam>
	/// <param name="value">The value to format.</param>
	extension<T>(T value) where T : ISpanFormattable
	{
		/// <summary>
		/// Attempts to format a value into a <see cref="Span{T}"/> of characters, starting at the given position, and
		/// advances the position by the number of characters written.
		/// </summary>
		/// <remarks>
		///     When formatting fails, <paramref name="position"/> is left unchanged, though
		///     <paramref name="destination"/> may hold partially written characters past it.
		/// </remarks>
		/// <param name="destination">The span that receives the formatted characters.</param>
		/// <param name="position">
		///     The index in <paramref name="destination"/> at which to start writing, advanced by the number of
		///     characters written if formatting succeeds.
		/// </param>
		/// <param name="format">The format to use, or an empty span for the type's default format.</param>
		/// <param name="provider">
		///     The culture-specific formatting information, or <see langword="null"/> for the current culture.
		/// </param>
		/// <returns>
		///     <see langword="true"/> if <paramref name="value"/> was formatted into the remaining space of
		///     <paramref name="destination"/>; otherwise, <see langword="false"/>.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when <paramref name="position"/> is negative or greater than the length of
		///     <paramref name="destination"/>.
		/// </exception>
		public bool TryCopyToSlice(Span<char> destination, ref int position, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
		{
			bool result = false;
			if (value.TryFormat(destination.Slice(position), out int written, format, provider))
			{
				position += written;
				result = true;
			}

			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool EnclosedInCore(ReadOnlySpan<char> readOnlySpan, char opening, char closing)
	{
		return readOnlySpan.Length >= 2 && opening == readOnlySpan[0] && closing == readOnlySpan[^1];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool FirstCharEquals(ReadOnlySpan<char> span, char value)
	{
		return !span.IsEmpty && span[0] == value;
	}
}
