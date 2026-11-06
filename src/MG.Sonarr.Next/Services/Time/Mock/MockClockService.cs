namespace MG.Sonarr.Next.Services.Time.Mock;

/// <summary>
/// Represents a clock whose values come from configurable callbacks, falling back to a base clock for any
/// callback left unset.
/// </summary>
/// <remarks>
/// <para>
/// Each property invokes its callback on every read, passing the base clock, so a callback can return a fixed
/// value, offset the base clock's value, or compute a new value each time.
/// </para>
/// <para>
/// The class adds no synchronization of its own, so it is as thread-safe as the base clock and the callbacks
/// it holds.
/// </para>
/// </remarks>
internal sealed class MockClockService : IClock
{
	private readonly IClock _baseClock;
	private readonly MockClockParameters _parameters;

	/// <summary>
	/// Gets the mocked local date and time.
	/// </summary>
	/// <value>
	/// The local <see cref="DateTimeOffset"/> value that the <see cref="MockClockParameters.GetNow"/> callback
	/// returns, or the base clock's <see cref="IClock.Now"/> when no callback was set.
	/// </value>
	public DateTimeOffset Now => _parameters.GetNow(_baseClock);

	/// <summary>
	/// Gets the mocked local date.
	/// </summary>
	/// <value>
	/// The local <see cref="DateTime"/> date value that the <see cref="MockClockParameters.GetToday"/> callback
	/// returns, or the base clock's <see cref="IClock.Today"/> when no callback was set.
	/// </value>
	public DateTime Today => _parameters.GetToday(_baseClock);

	/// <summary>
	/// Gets the mocked Coordinated Universal Time (UTC) date and time.
	/// </summary>
	/// <value>
	/// The UTC <see cref="DateTimeOffset"/> value that the <see cref="MockClockParameters.GetUtcNow"/> callback
	/// returns, or the base clock's <see cref="IClock.UtcNow"/> when no callback was set.
	/// </value>
	public DateTimeOffset UtcNow => _parameters.GetUtcNow(_baseClock);

	/// <summary>
	/// Initializes a new <see cref="MockClockService"/> instance from a base clock and the callbacks that
	/// override its values.
	/// </summary>
	/// <remarks>
	/// The constructor modifies <paramref name="parameters"/>: it sets each callback that is
	/// <see langword="null"/> to one that returns the matching value of the clock passed to it, and the instance
	/// keeps a reference to <paramref name="parameters"/> rather than a copy, so later changes to its callbacks
	/// change the values this clock returns.
	/// </remarks>
	/// <param name="baseClock">The clock that each callback receives, and whose values the defaults return.</param>
	/// <param name="parameters">The callbacks that resolve this clock's values.</param>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="baseClock"/> or <paramref name="parameters"/> is null.
	/// </exception>
	internal MockClockService(IClock baseClock, MockClockParameters parameters)
	{
		ArgumentNullException.ThrowIfNull(baseClock);
		ArgumentNullException.ThrowIfNull(parameters);

		SetParametersIfNull(parameters);

		_baseClock = baseClock;
		_parameters = parameters;
	}

	/// <summary>
	/// Gets the local date and time of the specified clock; the default <see cref="MockClockParameters.GetNow"/>
	/// callback.
	/// </summary>
	/// <param name="baseClock">The clock to read.</param>
	/// <returns>The <see cref="IClock.Now"/> value of <paramref name="baseClock"/>.</returns>
	private static DateTimeOffset GetDefaultGetNow(IClock baseClock)
	{
		return baseClock.Now;
	}

	/// <summary>
	/// Gets the local date of the specified clock; the default <see cref="MockClockParameters.GetToday"/>
	/// callback.
	/// </summary>
	/// <param name="baseClock">The clock to read.</param>
	/// <returns>The <see cref="IClock.Today"/> value of <paramref name="baseClock"/>.</returns>
	private static DateTime GetDefaultToday(IClock baseClock)
	{
		return baseClock.Today;
	}

	/// <summary>
	/// Gets the UTC date and time of the specified clock; the default
	/// <see cref="MockClockParameters.GetUtcNow"/> callback.
	/// </summary>
	/// <param name="baseClock">The clock to read.</param>
	/// <returns>The <see cref="IClock.UtcNow"/> value of <paramref name="baseClock"/>.</returns>
	private static DateTimeOffset GetDefaultUtcNow(IClock baseClock)
	{
		return baseClock.UtcNow;
	}

	/// <summary>
	/// Sets each <see langword="null"/> callback of the specified parameters to the default callback that reads
	/// the matching value from the base clock.
	/// </summary>
	/// <param name="parameters">The parameters to update in place.</param>
	private static void SetParametersIfNull(MockClockParameters parameters)
	{
		if (parameters.GetNow is null)
		{
			parameters.GetNow = GetDefaultGetNow;
		}

		if (parameters.GetToday is null)
		{
			parameters.GetToday = GetDefaultToday;
		}

		if (parameters.GetUtcNow is null)
		{
			parameters.GetUtcNow = GetDefaultUtcNow;
		}
	}
}
