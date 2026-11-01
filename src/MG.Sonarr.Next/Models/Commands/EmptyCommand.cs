namespace MG.Sonarr.Next.Models.Commands;

/// <summary>
/// Represents the command that stands for no command.
/// </summary>
/// <remarks>
/// Every instance is the same: an ID of 0, the name <c>N/A</c>, a start time of <see cref="DateTimeOffset.MinValue"/>,
/// no end time, and never completed.
/// </remarks>
internal readonly struct EmptyCommand : ICommand
{
	/// <summary>
	/// The empty command's name.
	/// </summary>
	const string NA = "N/A";

	/// <summary>
	/// Gets the time that the command ended.
	/// </summary>
	/// <value>Always <see langword="null"/>.</value>
	public DateTimeOffset? Ended => null;
	/// <summary>
	/// Gets the command's ID.
	/// </summary>
	/// <value>Always 0.</value>
	public int Id => 0;
	/// <summary>
	/// Gets a value that indicates whether the command has completed.
	/// </summary>
	/// <value>Always <see langword="false"/>.</value>
	public bool IsCompleted => false;
	/// <summary>
	/// Gets the command's name.
	/// </summary>
	/// <value>Always <c>N/A</c>.</value>
	public string Name => NA;
	/// <summary>
	/// Gets the time that the command started.
	/// </summary>
	/// <value>Always <see cref="DateTimeOffset.MinValue"/>.</value>
	public DateTimeOffset Started => DateTimeOffset.MinValue;

	/// <summary>
	/// The empty command.
	/// </summary>
	internal static readonly EmptyCommand Default = default;

	/// <summary>
	/// Compares the empty command with another command by ID.
	/// </summary>
	/// <param name="other">The command to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if 0 comes before the ID of <paramref name="other"/>, 0 if that ID is 0, and a positive
	/// number if 0 comes after it or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(ICommand? other)
	{
		return Comparer<int?>.Default.Compare(this.Id, other?.Id);
	}

	/// <summary>
	/// Determines whether another command is the empty command.
	/// </summary>
	/// <param name="other">The command to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="other"/> is an <see cref="EmptyCommand"/>, or has an ID of 0, a start
	/// time of <see cref="DateTimeOffset.MinValue"/>, no end time, and hasn't completed; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public bool Equals(ICommand? other)
	{
		if (other is EmptyCommand)
		{
			return true;
		}

		return this.Id.IsEqualTo(other?.Id)
			   &&
			   this.Started == other.Started
			   &&
			   !other.Ended.HasValue
			   &&
			   !other.IsCompleted;
	}
	/// <summary>
	/// Determines whether an object is the empty command.
	/// </summary>
	/// <param name="obj">The object to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="obj"/> is an <see cref="EmptyCommand"/>, or an <see cref="ICommand"/>
	/// that <see cref="Equals(ICommand)"/> finds equal; otherwise, <see langword="false"/>.
	/// </returns>
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return obj switch
		{
			EmptyCommand => true,
			ICommand command => this.Equals(command),
			_ => false,
		};
	}
	/// <summary>
	/// Gets the empty command's hash code.
	/// </summary>
	/// <returns>The same hash code for every instance.</returns>
	public override int GetHashCode()
	{
		return HashCode.Combine(
			this.Id,
			DateTimeOffset.MinValue,
			this.IsCompleted,
			this.Started);
	}
}
