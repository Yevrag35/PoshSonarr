namespace MG.Sonarr.Next.Json.Naming;

/// <summary>
/// Provides a naming policy for converting JSON property names to camel case using spans.
/// </summary>
/// <remarks>
/// <para>
/// The span overloads change a name in place by the rules of <see cref="JsonNamingPolicy.CamelCase"/>: a name whose
/// first character isn't uppercase stays as it is, and otherwise the leading run of uppercase characters is
/// lowercased, except for the last one when a lowercase character follows it, so <c>IMDbId</c> becomes
/// <c>imDbId</c> and <c>ID</c> becomes <c>id</c>.
/// </para>
/// <para>
/// The UTF-8 overload changes only ASCII letters, so its result can differ from the policy's for a name whose leading
/// run holds an uppercase letter outside ASCII, such as <c>Été</c>, which it leaves as it is.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class JsonSpanCamelCaseNamingPolicy : JsonNamingPolicy
{
	private static readonly JsonNamingPolicy s_camelCase = CamelCase;
	/// <summary>
	/// Gets the singleton instance of the <see cref="JsonSpanCamelCaseNamingPolicy"/> class.
	/// </summary>
	public static readonly JsonSpanCamelCaseNamingPolicy SpanPolicy = new();

	/// <summary>
	/// Converts the specified name to camel case.
	/// </summary>
	/// <param name="name">The name to convert.</param>
	/// <returns>The camel case version of the name.</returns>
	public override string ConvertName(string name)
	{
		return s_camelCase.ConvertName(name);
	}

	/// <summary>
	/// Converts the specified span of characters to camel case.
	/// </summary>
	/// <param name="span">The span of characters to convert.</param>
	/// <returns>The camel case version of the span.</returns>
	public Span<char> ConvertName(Span<char> span)
	{
		FixCasing(span);
		return span;
	}

	/// <summary>
	/// Converts the specified span of UTF-8 bytes to camel case.
	/// </summary>
	/// <remarks>
	/// Only ASCII letters change, so the span keeps its length, and a name whose leading run holds an uppercase letter
	/// outside ASCII doesn't get that letter lowercased, as it would through <see cref="ConvertName(string)"/>.
	/// </remarks>
	/// <param name="utf8Text">The span of UTF-8 bytes to convert.</param>
	/// <returns>The camel case version of the span.</returns>
	[SuppressMessage("Style", "CA1822", Justification = "This method is an instance method to match the signature of the base class.")]
	public Span<byte> ConvertName(Span<byte> utf8Text)
	{
		FixCasing(utf8Text);
		return utf8Text;
	}

	/// <summary>
	/// Converts the initial characters of the specified span to lowercase by the rules of
	/// <see cref="JsonNamingPolicy.CamelCase"/>.
	/// </summary>
	/// <remarks>This method updates the contents of the provided span directly. A span whose first character isn't
	/// uppercase is left as it is, as the policy leaves such a name. Otherwise only the first few characters are
	/// affected, based on the casing and spacing of the input. The operation stops when a non-uppercase character or a
	/// space is encountered after the first character.</remarks>
	/// <param name="chars">A span of characters whose initial casing will be modified in place. The span must be writable.</param>
	private static void FixCasing(Span<char> chars)
	{
		// JsonNamingPolicy.CamelCase returns a name whose first character isn't uppercase as it is, so aB stays aB.
		if (chars.IsEmpty || !char.IsUpper(chars[0]))
		{
			return;
		}

		unsafe
		{
			// SAFETY: The early return leaves at least one character, so Ref() is in range. Offset(i) runs in the loop
			// body, where i < chars.Length, and each Offset(next) runs only once next < chars.Length has held.
			ref char c = ref chars.Ref();
			for (int i = 0; i < chars.Length; i++)
			{
				if (i == 1 && !char.IsUpper(c.Offset(i))) // JIT loses track of i here, for some reason,
				{                                         // so we help it out for just this one index access with i.
					break;
				}

				int next = i + 1;

				// Stop when next char is already lowercase.
				if (i > 0 && next < chars.Length && !char.IsUpper(c.Offset(next)))
				{
					// If the next char is a space, lowercase current char before exiting.
					if (c.Offset(next) == ' ')
					{
						chars[i] = char.ToLowerInvariant(chars[i]);
					}

					break;
				}

				chars[i] = char.ToLowerInvariant(chars[i]);
			}
		}
	}

	/// <summary>
	/// Converts the initial bytes of the specified UTF-8 span to lowercase, applying the same camel-case rules as
	/// <see cref="FixCasing(Span{char})"/>.
	/// </summary>
	/// <remarks>This method updates the contents of the provided span directly. A span whose first byte isn't an ASCII
	/// uppercase letter is left as it is. Otherwise only the leading run of ASCII uppercase letters is affected; the
	/// operation stops when a non-uppercase byte or a space is encountered after the first byte. Bytes outside the ASCII
	/// uppercase range (including every byte of a multi-byte UTF-8 sequence) are left untouched, so the length of the
	/// span never changes.</remarks>
	/// <param name="utf8Bytes">A span of UTF-8 bytes whose initial casing is modified in place. The span must be writable.</param>
	private static void FixCasing(Span<byte> utf8Bytes)
	{
		if (utf8Bytes.IsEmpty || !IsAsciiUpper(utf8Bytes[0]))
		{
			return;
		}

		unsafe
		{
			// SAFETY: The early return leaves at least one byte, so Ref() is in range. Each Offset(i) runs in the loop
			// body, where i < utf8Bytes.Length, and each Offset(next) runs only once next < utf8Bytes.Length has held.
			ref byte firstByte = ref utf8Bytes.Ref();
			for (int i = 0; i < utf8Bytes.Length; i++)
			{
				if (i == 1 && !IsAsciiUpper(firstByte.Offset(i)))
				{
					break;
				}

				int next = i + 1;

				// Stop when the next byte is already lowercase (or begins a non-ASCII sequence).
				if (i > 0 && next < utf8Bytes.Length && !IsAsciiUpper(firstByte.Offset(next)))
				{
					// If the next byte is a space, lowercase the current byte before exiting.
					const byte space = (byte)' ';

					if (firstByte.Offset(next) == space)
					{
						firstByte.Offset(i) = ToAsciiLower(firstByte.Offset(i));
					}

					break;
				}

				firstByte.Offset(i) = ToAsciiLower(firstByte.Offset(i));
			}
		}
	}

	/// <summary>
	/// Determines whether the specified byte is an ASCII uppercase letter.
	/// </summary>
	/// <param name="b">The byte to test.</param>
	/// <returns><see langword="true"/> if <paramref name="b"/> is in the range <c>A</c> to <c>Z</c> inclusive; otherwise, <see langword="false"/>.</returns>
	private static bool IsAsciiUpper(byte b)
	{
		return b is >= (byte)'A' and <= (byte)'Z';
	}

	/// <summary>
	/// Converts the specified byte to its ASCII lowercase form, leaving any byte that is not an ASCII uppercase letter unchanged.
	/// </summary>
	/// <param name="b">The byte to convert.</param>
	/// <returns>The ASCII lowercase form of <paramref name="b"/> when it is an uppercase letter; otherwise, <paramref name="b"/> unchanged.</returns>
	private static byte ToAsciiLower(byte b)
	{
		return IsAsciiUpper(b) ? (byte)(b + 32) : b;
	}
}