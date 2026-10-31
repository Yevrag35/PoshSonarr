using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Services.Http;
using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Models.System;

/// <summary>
/// Represents the outcome of a ping to the Sonarr server.
/// </summary>
public sealed class PingResult
{
	/// <summary>
	/// Gets a value that indicates whether the ping succeeded.
	/// </summary>
	/// <value><see langword="true"/> if the ping's request didn't fail; otherwise, <see langword="false"/>.</value>
	public bool IsSuccess { get; }
	/// <summary>
	/// Gets how long the ping took.
	/// </summary>
	/// <value>The time from sending the ping to receiving its answer.</value>
	public TimeSpan Duration { get; }
	/// <summary>
	/// Gets the response that the server sent.
	/// </summary>
	/// <value>The response, such as <c>pong</c>, or an empty string when the ping got no body.</value>
	public string Message { get; }
	/// <summary>
	/// Gets the HTTP status of the ping's answer.
	/// </summary>
	/// <value>
	/// The status code's number followed by its name in parentheses, such as <c>200 (OK)</c>, or only the name for a
	/// code that's below 0 or above 999.
	/// </value>
	public string Status { get; }
	/// <summary>
	/// Gets the error that the ping's request failed with.
	/// </summary>
	/// <value>The error record for a failed ping, or <see langword="null"/> for one that succeeded.</value>
	public ErrorRecord? Error { get; }

	/// <summary>
	/// Initializes a new <see cref="PingResult"/> instance from the result of a ping's request and the time it took.
	/// </summary>
	/// <param name="response">The result of the ping's request.</param>
	/// <param name="elapsedTicks">How long the ping took, in ticks of 100 nanoseconds.</param>
	public PingResult(SonarrClientResult<PingResponse> response, long elapsedTicks)
	{
		this.IsSuccess = !response.IsError;
		this.Duration = TimeSpan.FromTicks(elapsedTicks);

		this.Message = response.Value?.Response ?? string.Empty;
		this.Status = GetHttpStatusString(response.StatusCode);
		this.Error = response.Error;
	}

	/// <summary>
	/// Formats a status code as its number followed by its name in parentheses.
	/// </summary>
	/// <param name="code">The status code to format.</param>
	/// <returns>
	/// The code's number and name, such as <c>404 (NotFound)</c>, or only the name for a code that's below 0 or above
	/// 999.
	/// </returns>
	private static string GetHttpStatusString(HttpStatusCode code)
	{
		string codeStr = code.ToString();
		int asInt = (int)code;

		if (asInt > 999 || asInt < 0)
		{
			return codeStr;
		}

		using SpanStringBuilder builder = new(codeStr.Length + 6);
		builder.Append(asInt, LengthConstants.INT_MAX, format: default, Statics.DefaultProvider);
		builder.AppendChars(' ', '(');
		builder.Append(codeStr);
		builder.Append(')');

		return builder.ToString();
	}
}
