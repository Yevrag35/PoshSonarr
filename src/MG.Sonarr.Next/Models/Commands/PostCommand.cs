namespace MG.Sonarr.Next.Models.Commands;

/// <summary>
/// Represents the request body that starts a Sonarr command.
/// </summary>
public sealed class PostCommand
{
	/// <summary>
	/// Gets the command's display name.
	/// </summary>
	/// <value>The command's display name, such as <c>Rss Sync</c>.</value>
	public required string CommandName { get; init; }
	/// <summary>
	/// Gets the name of the command to start.
	/// </summary>
	/// <value>The name that Sonarr knows the command by, such as <c>RssSync</c>.</value>
	public required string Name { get; init; }
	/// <summary>
	/// Gets the priority that Sonarr gives the command.
	/// </summary>
	/// <value>The command's priority. The default is <see cref="CommandPriority.Normal"/>.</value>
	public CommandPriority Priority { get; init; } = CommandPriority.Normal;
	/// <summary>
	/// Gets a value that indicates whether Sonarr updates the scheduled task that runs the command.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if Sonarr updates the command's scheduled task; otherwise, <see langword="false"/>. The
	/// default is <see langword="true"/>.
	/// </value>
	public bool UpdateScheduledTask { get; init; } = true;
}
