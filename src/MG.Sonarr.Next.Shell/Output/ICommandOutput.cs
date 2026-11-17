using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models;

namespace MG.Sonarr.Next.Shell.Output;

/// <summary>
/// Represents the properties of the commands that <c>Get-SonarrCommand</c> writes, for the cmdlet's
/// <see cref="OutputTypeAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// No model implements the interface. The cmdlet writes each command as a
/// <see cref="MG.Sonarr.Next.Models.Commands.CommandObject"/>, whose PowerShell properties come from the fields of
/// Sonarr's JSON for a command, and the interface names the ones that a command has. The <c>Priority</c> and
/// <c>Trigger</c> values get an uppercase first letter.
/// </para>
/// <para>
/// The <c>Id</c> property is the command's ID, and the <c>MetadataTag</c> property is the command's tag.
/// </para>
/// </remarks>
internal interface ICommandOutput : IHasId, IJsonSonarrMetadata
{
	/// <summary>
	/// Gets the user agent of the client that started the command.
	/// </summary>
	/// <value>The client's user agent.</value>
	string ClientUserAgent { get; }
	/// <summary>
	/// Gets the name that Sonarr shows for the command.
	/// </summary>
	/// <value>The command's display name.</value>
	string CommandName { get; }
	/// <summary>
	/// Gets how long the command ran.
	/// </summary>
	/// <value>The command's run time, or <see langword="null"/> if it hasn't ended.</value>
	TimeSpan? Duration { get; }
	/// <summary>
	/// Gets the time that the command ended.
	/// </summary>
	/// <value>The time that the command ended, or <see langword="null"/> if it hasn't ended.</value>
	DateTimeOffset? Ended { get; }
	/// <summary>
	/// Gets the command's latest message.
	/// </summary>
	/// <value>The message that the command last reported.</value>
	string Message { get; }
	/// <summary>
	/// Gets the command's priority.
	/// </summary>
	/// <value>The command's priority, such as <c>Normal</c> or <c>High</c>.</value>
	string Priority { get; }
	/// <summary>
	/// Gets the time that the command was queued.
	/// </summary>
	/// <value>The time that Sonarr queued the command.</value>
	DateTimeOffset Queued { get; }
	/// <summary>
	/// Gets a value that indicates whether Sonarr sends the client updates on the command's progress.
	/// </summary>
	/// <value><see langword="true"/> if Sonarr sends the client updates; otherwise, <see langword="false"/>.</value>
	bool SendUpdatesToClient { get; }
	/// <summary>
	/// Gets the time that the command started.
	/// </summary>
	/// <value>The time that the command started.</value>
	DateTimeOffset Started { get; }
	/// <summary>
	/// Gets the time that the command's status last changed.
	/// </summary>
	/// <value>The time of the last status change, or <see langword="null"/> if Sonarr sent none.</value>
	DateTimeOffset? StateChangeTime { get; }
	/// <summary>
	/// Gets what started the command.
	/// </summary>
	/// <value>The command's trigger, such as <c>Manual</c> or <c>Scheduled</c>.</value>
	string Trigger { get; }
	/// <summary>
	/// Gets a value that indicates whether running the command updates its scheduled task's last run time.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the command updates its scheduled task; otherwise, <see langword="false"/>.
	/// </value>
	bool UpdateScheduledTask { get; }
}
