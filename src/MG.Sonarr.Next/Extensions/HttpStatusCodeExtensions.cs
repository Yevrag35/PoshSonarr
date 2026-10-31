using MG.Sonarr.Next.Extensions.Strings;
using System.Collections.ObjectModel;
using System.Net;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Custom extension methods for <see cref="HttpStatusCode"/> values.
/// </summary>
public static class HttpStatusCodeExtensions
{
	#region CACHING
	static readonly Lazy<ReadOnlyDictionary<int, string>> _codes = new(GetLookupCodes);
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
	/// Returns the <see cref="string"/> representation of the current 
	/// <see cref="HttpStatusCode"/> value formatted as: <code>int (string)</code>
	/// </summary>
	/// <remarks>
	///     An example is converting <see cref="HttpStatusCode.BadRequest"/> to:
	///     <code>400 (BadRequest)</code>
	///     A code that <see cref="HttpStatusCode"/> doesn't name, such as 520, is written as its number alone.
	/// </remarks>
	/// <param name="statusCode">The status code to convert.</param>
	/// <returns>
	///     The <see cref="string"/> response form of the <see cref="HttpStatusCode"/> value.
	/// </returns>
	/// <example>200 (OK)</example>
	public static string ToResponseString(this HttpStatusCode statusCode)
	{
		return _codes.Value.TryGetValue((int)statusCode, out string? value)
			? value
			: statusCode.ToString();
	}

	/// <summary>
	/// Tries to format the response <see cref="string"/> value of the current 
	/// <see cref="HttpStatusCode"/> instance into the 
	/// provided span of characters.
	/// </summary>
	/// <remarks>
	///     An example is writing <see cref="HttpStatusCode.BadRequest"/> as:
	///     <code>400 (BadRequest)</code>
	///     A code that <see cref="HttpStatusCode"/> doesn't name, such as 520, is written as its number alone, as
	///     <see cref="ToResponseString(HttpStatusCode)"/> writes it.
	/// </remarks>
	/// <param name="statusCode">The status code to format.</param>
	/// <param name="destination">
	///     The span in which to write this value formatted as a span of characters.
	/// </param>
	/// <param name="charsWritten">
	///     When this method returns, contains the number of characters that were written in
	///     <paramref name="destination"/>
	/// </param>
	/// <returns>
	///     <see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/> when
	///     <paramref name="destination"/> is too short.
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
