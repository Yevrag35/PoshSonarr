namespace MG.Sonarr.Next.Services.Time.Mock;

/// <summary>
/// Defines callbacks that customize mocked clock values.
/// </summary>
public interface IMockClockParameters
{
	/// <summary>
	/// Sets the callback that resolves the mocked local date and time.
	/// </summary>
	/// <value>The callback that receives the base <see cref="IClock"/> and returns a local <see cref="DateTimeOffset"/> value.</value>
	Func<IClock, DateTimeOffset> GetNow { set; }

	/// <summary>
	/// Sets the callback that resolves the mocked local date.
	/// </summary>
	/// <value>The callback that receives the base <see cref="IClock"/> and returns a local <see cref="DateTime"/> value.</value>
	Func<IClock, DateTime> GetToday { set; }

	/// <summary>
	/// Sets the callback that resolves the mocked UTC date and time.
	/// </summary>
	/// <value>The callback that receives the base <see cref="IClock"/> and returns a UTC <see cref="DateTimeOffset"/> value.</value>
	Func<IClock, DateTimeOffset> GetUtcNow { set; }
}

/// <summary>
/// Represents the default callback container for mocked clock values.
/// </summary>
internal sealed class MockClockParameters : IMockClockParameters
{
	/// <summary>
	/// Gets or sets the callback that resolves the mocked local date and time.
	/// </summary>
	/// <value>The callback that returns a local <see cref="DateTimeOffset"/> value.</value>
	public Func<IClock, DateTimeOffset> GetNow { get; set; } = null!;

	/// <summary>
	/// Gets or sets the callback that resolves the mocked local date.
	/// </summary>
	/// <value>The callback that returns a local <see cref="DateTime"/> value.</value>
	public Func<IClock, DateTime> GetToday { get; set; } = null!;

	/// <summary>
	/// Gets or sets the callback that resolves the mocked UTC date and time.
	/// </summary>
	/// <value>The callback that returns a UTC <see cref="DateTimeOffset"/> value.</value>
	public Func<IClock, DateTimeOffset> GetUtcNow { get; set; } = null!;
}
