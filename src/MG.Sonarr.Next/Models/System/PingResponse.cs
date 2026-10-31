using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.System;

/// <summary>
/// Represents the body of the Sonarr server's answer to a ping.
/// </summary>
public sealed class PingResponse
{
	/// <summary>
	/// The response that a server that's up sends.
	/// </summary>
	const string PONG = "pong";

	/// <summary>
	/// Gets the response that the server sent.
	/// </summary>
	/// <value>The <c>response</c> field of the body, or an empty string when the body has none.</value>
	public string Response { get; init; } = string.Empty;

	/// <summary>
	/// Gets a value that indicates whether the server answered <c>pong</c>.
	/// </summary>
	/// <remarks>
	/// The response compares ordinally, ignoring case, and the property isn't serialized.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if <see cref="Response"/> is <c>pong</c>, ignoring case; otherwise,
	/// <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(Response))]
	[JsonIgnore(Condition = JsonIgnoreCondition.Always)]
	public bool IsPong => PONG.Equals(this.Response, StringComparison.OrdinalIgnoreCase);
}
