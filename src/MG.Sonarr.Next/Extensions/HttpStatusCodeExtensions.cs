using MG.Sonarr.Next.Extensions.Strings;
using System.Collections.ObjectModel;
using System.Net;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods that write an <see cref="HttpStatusCode"/> as response text, such as
/// <c>400 (BadRequest)</c>.
/// </summary>
/// <remarks>
/// The text for every code that <see cref="HttpStatusCode"/> names is built once, on first use, and cached.
/// </remarks>
public static class HttpStatusCodeExtensions
{
	#region CACHING
	/// <summary>
	/// The cached response text of each named status code, keyed by its number.
	/// </summary>
	static readonly Lazy<ReadOnlyDictionary<int, string>> _codes = new(GetLookupCodes);
	/// <summary>
	/// Builds the response text of every status code that <see cref="HttpStatusCode"/> names.
	/// </summary>
	/// <remarks>
	/// A number that several names share, such as 302 for <see cref="HttpStatusCode.Found"/> and
	/// <see cref="HttpStatusCode.Redirect"/>, gets one entry, with the name that <see cref="Enum.ToString()"/> gives
	/// it.
	/// </remarks>
	/// <returns>A read-only dictionary of response text, keyed by status code number.</returns>
	static ReadOnlyDictionary<int, string> GetLookupCodes()
	{
		HttpStatusCode[] codes = Enum.GetValues<HttpStatusCode>();
		Dictionary<int, string> dict = new(codes.Length);

		foreach (HttpStatusCode code in codes)
		{
			_ = dict.TryAdd((int)code, ToResponseStringFormat(in code));
		}

		return new ReadOnlyDictionary<int, string>(dict);
	}
	/// <summary>
	/// Writes a named status code as its number, a space, and its name in parentheses.
	/// </summary>
	/// <remarks>
	/// The text's length assumes a three-digit number, which every named status code has.
	/// </remarks>
	/// <param name="statusCode">The status code to write, which <see cref="HttpStatusCode"/> names.</param>
	/// <returns>The response text, such as <c>400 (BadRequest)</c>.</returns>
	static string ToResponseStringFormat(in HttpStatusCode statusCode)
	{

		string codeStr = statusCode.ToString();

		return string.Create(codeStr.Length + 6, (codeStr, statusCode), (chars, state) =>
		{
			_ = ((int)state.statusCode).TryFormat(chars, out int written, default, Statics.DefaultProvider);
			written += chars.Slice(written).Push(' ', '(');

			written = codeStr.CopyToSlice(chars, written);
			chars[written++] = ')';

			Debug.Assert(written == chars.Length, "All characters should have been written.");
		});
	}

	#endregion

	/// <summary>
	/// Gets the response text of a status code, which is its number followed by its name in parentheses.
	/// </summary>
	/// <remarks>
	/// <para><see cref="HttpStatusCode.BadRequest"/> is written as <c>400 (BadRequest)</c>, and
	/// <see cref="HttpStatusCode.OK"/> as <c>200 (OK)</c>.</para>
	/// <para>A code that <see cref="HttpStatusCode"/> doesn't name, such as 520, is written as its number alone.</para>
	/// </remarks>
	/// <param name="statusCode">The status code to write.</param>
	/// <returns>The response text of <paramref name="statusCode"/>.</returns>
	public static string ToResponseString(this HttpStatusCode statusCode)
	{
		return _codes.Value.TryGetValue((int)statusCode, out string? value)
			? value
			: statusCode.ToString();
	}

	/// <summary>
	/// Tries to write the response text of a status code into a span of characters.
	/// </summary>
	/// <remarks>
	/// <para>The text is the one that <see cref="ToResponseString(HttpStatusCode)"/> returns:
	/// <see cref="HttpStatusCode.BadRequest"/> is written as <c>400 (BadRequest)</c>, and a code that
	/// <see cref="HttpStatusCode"/> doesn't name, such as 520, as its number alone.</para>
	/// </remarks>
	/// <param name="statusCode">The status code to write.</param>
	/// <param name="destination">The span to write the text into.</param>
	/// <param name="charsWritten">
	/// When this method returns, the number of characters written into <paramref name="destination"/>, or 0 when it
	/// returns <see langword="false"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the text was written; <see langword="false"/> if <paramref name="destination"/> is
	/// too short to hold it.
	/// </returns>
	public static bool TryFormatAsResponse(this HttpStatusCode statusCode, Span<char> destination, out int charsWritten)
	{
		if (!_codes.Value.TryGetValue((int)statusCode, out string? value))
		{
			return Enum.TryFormat(statusCode, destination, out charsWritten);
		}

		charsWritten = 0;
		bool result = value.TryCopyTo(destination);
		if (result)
		{
			charsWritten += value.Length;
		}

		return result;
	}
}
