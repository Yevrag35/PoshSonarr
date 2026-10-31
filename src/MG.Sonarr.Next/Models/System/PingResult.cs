using MG.Sonarr.Next.Services.Http;
using System.Management.Automation;

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
	/// The status code's number followed by its standard reason phrase in parentheses, such as <c>200 (OK)</c> or
	/// <c>404 (Not Found)</c>, the number alone for a code without a standard phrase, or <c>No response</c> when the ping
	/// got no response.
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
		this.Status = response.StatusCode.ToResponseString();
		this.Error = response.Error;
	}
}
