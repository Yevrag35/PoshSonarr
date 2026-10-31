using MG.Sonarr.Next.Services.Time.Mock;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Time;

/// <summary>
/// Represents the default system clock implementation.
/// </summary>
file sealed class ClockService : IClock
{
	/// <summary>
	/// Gets the current local date and time.
	/// </summary>
	/// <value>The current local <see cref="DateTimeOffset"/> value.</value>
	public DateTimeOffset Now => DateTimeOffset.Now;

	/// <summary>
	/// Gets the current local date.
	/// </summary>
	/// <value>The current local <see cref="DateTime"/> date value.</value>
	public DateTime Today => DateTime.Today;

	/// <summary>
	/// Gets the current Coordinated Universal Time (UTC) date and time.
	/// </summary>
	/// <value>The current UTC <see cref="DateTimeOffset"/> value.</value>
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

	/// <summary>
	/// Initializes a new <see cref="ClockService"/> instance.
	/// </summary>
	public ClockService() { }
}

/// <summary>
/// Provides dependency injection registrations for clock services.
/// </summary>
public static class ClockServiceDepedencyInjection
{
	/// <summary>
	/// Adds the default system clock service to the specified service collection.
	/// </summary>
	/// <param name="services">The service collection to update.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
	public static IServiceCollection AddClock(this IServiceCollection services)
	{
		return services.AddSingleton<IClock, ClockService>();
	}

	/// <summary>
	/// Adds a mockable clock service to the specified service collection.
	/// </summary>
	/// <param name="services">The service collection to update.</param>
	/// <param name="setupCallbacks">The callback that configures mocked clock behavior.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
	public static IServiceCollection AddClock(this IServiceCollection services, Action<IMockClockParameters> setupCallbacks)
	{
		ClockService baseClock = new();
		MockClockParameters parameters = new();
		setupCallbacks(parameters);

		return services.AddSingleton<IClock>(new MockClockService(baseClock, parameters));
	}
}
