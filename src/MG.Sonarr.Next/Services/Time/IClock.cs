namespace MG.Sonarr.Next.Services.Time;

/// <summary>
/// Defines members that expose the current local and UTC date and time.
/// </summary>
public interface IClock
{
	/// <summary>
	/// Gets the current local date and time.
	/// </summary>
	/// <value>The current local <see cref="DateTimeOffset"/> value.</value>
	DateTimeOffset Now { get; }

	/// <summary>
	/// Gets the current local date.
	/// </summary>
	/// <value>The current local <see cref="DateTime"/> date value.</value>
	DateTime Today { get; }

	/// <summary>
	/// Gets the current Coordinated Universal Time (UTC) date and time.
	/// </summary>
	/// <value>The current UTC <see cref="DateTimeOffset"/> value.</value>
	DateTimeOffset UtcNow { get; }
}
