namespace MG.Sonarr.Next.Models.Commands;

/// <summary>
/// Represents a Sonarr command as the command history keeps it.
/// </summary>
public interface ICommand : IComparable<ICommand>, IEquatable<ICommand>
{
	/// <summary>
	/// Gets the time that the command ended.
	/// </summary>
	/// <value>The time that the command ended, or <see langword="null"/> if it hasn't ended.</value>
	DateTimeOffset? Ended { get; }
	/// <summary>
	/// Gets the command's ID.
	/// </summary>
	/// <value>The ID that Sonarr gave the command.</value>
	int Id { get; }
	/// <summary>
	/// Gets a value that indicates whether the command has completed.
	/// </summary>
	/// <remarks>
	/// A completed command has an end time, so <see cref="Ended"/> isn't <see langword="null"/> when this is
	/// <see langword="true"/>.
	/// </remarks>
	/// <value><see langword="true"/> if the command has completed; otherwise, <see langword="false"/>.</value>
	[MemberNotNullWhen(true, nameof(Ended))]
	bool IsCompleted { get; }
	/// <summary>
	/// Gets the command's name.
	/// </summary>
	/// <value>The command's name.</value>
	string Name { get; }
	/// <summary>
	/// Gets the time that the command started.
	/// </summary>
	/// <value>The time that the command started.</value>
	DateTimeOffset Started { get; }
}
