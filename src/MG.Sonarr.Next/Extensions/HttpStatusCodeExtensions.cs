using MG.Sonarr.Next.Services.Http.Handlers;
using System.Net;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides an extension property that gets the status text that the module shows a person for an
/// <see cref="HttpStatusCode"/>, such as <c>404 (Not Found)</c>.
/// </summary>
internal static class HttpStatusCodeExtensions
{
	/// <summary>
	/// Provides the status text of an <see cref="HttpStatusCode"/>.
	/// </summary>
	/// <param name="statusCode">The status code that the property describes.</param>
	extension(HttpStatusCode statusCode)
	{
		/// <summary>
		/// Gets the status text of <paramref name="statusCode"/>, which is its number followed by its standard reason
		/// phrase in parentheses.
		/// </summary>
		/// <remarks>
		/// <para>The phrases are the ones that <see cref="HttpResponseMessage.ReasonPhrase"/> gives a response that
		/// carries none of its own, which are also the ones that Sonarr sends. So <see cref="HttpStatusCode.BadRequest"/>
		/// is written as <c>400 (Bad Request)</c>, not with its enum name, and a number that several enum names share,
		/// such as 302 for <see cref="HttpStatusCode.Found"/> and <see cref="HttpStatusCode.Redirect"/>, has one
		/// text.</para>
		/// <para>A code without a standard phrase, such as 306 or the 520 that a proxy can send, is written as its number
		/// alone, formatted with <see cref="CultureInfo.CurrentCulture"/> as it is when the property is read. The code
		/// that the module gives a request that got no response, 600, is written as <c>No response</c>.</para>
		/// <para>Every other text is a string literal, so reading the property allocates only for a code without a
		/// standard phrase.</para>
		/// </remarks>
		/// <value>The status text, such as <c>404 (Not Found)</c>.</value>
		public string AsResponseString => (int)statusCode switch
		{
			100 => "100 (Continue)",
			101 => "101 (Switching Protocols)",
			102 => "102 (Processing)",
			103 => "103 (Early Hints)",
			200 => "200 (OK)",
			201 => "201 (Created)",
			202 => "202 (Accepted)",
			203 => "203 (Non-Authoritative Information)",
			204 => "204 (No Content)",
			205 => "205 (Reset Content)",
			206 => "206 (Partial Content)",
			207 => "207 (Multi-Status)",
			208 => "208 (Already Reported)",
			226 => "226 (IM Used)",
			300 => "300 (Multiple Choices)",
			301 => "301 (Moved Permanently)",
			302 => "302 (Found)",
			303 => "303 (See Other)",
			304 => "304 (Not Modified)",
			305 => "305 (Use Proxy)",
			307 => "307 (Temporary Redirect)",
			308 => "308 (Permanent Redirect)",
			400 => "400 (Bad Request)",
			401 => "401 (Unauthorized)",
			402 => "402 (Payment Required)",
			403 => "403 (Forbidden)",
			404 => "404 (Not Found)",
			405 => "405 (Method Not Allowed)",
			406 => "406 (Not Acceptable)",
			407 => "407 (Proxy Authentication Required)",
			408 => "408 (Request Timeout)",
			409 => "409 (Conflict)",
			410 => "410 (Gone)",
			411 => "411 (Length Required)",
			412 => "412 (Precondition Failed)",
			413 => "413 (Request Entity Too Large)",
			414 => "414 (Request-Uri Too Long)",
			415 => "415 (Unsupported Media Type)",
			416 => "416 (Requested Range Not Satisfiable)",
			417 => "417 (Expectation Failed)",
			421 => "421 (Misdirected Request)",
			422 => "422 (Unprocessable Entity)",
			423 => "423 (Locked)",
			424 => "424 (Failed Dependency)",
			426 => "426 (Upgrade Required)",
			428 => "428 (Precondition Required)",
			429 => "429 (Too Many Requests)",
			431 => "431 (Request Header Fields Too Large)",
			451 => "451 (Unavailable For Legal Reasons)",
			500 => "500 (Internal Server Error)",
			501 => "501 (Not Implemented)",
			502 => "502 (Bad Gateway)",
			503 => "503 (Service Unavailable)",
			504 => "504 (Gateway Timeout)",
			505 => "505 (Http Version Not Supported)",
			506 => "506 (Variant Also Negotiates)",
			507 => "507 (Insufficient Storage)",
			508 => "508 (Loop Detected)",
			510 => "510 (Not Extended)",
			511 => "511 (Network Authentication Required)",
			(int)ErrorHandler.NoResponseCode => "No response",
			_ => ((int)statusCode).ToString(CultureInfo.CurrentCulture),
		};
	}
}
