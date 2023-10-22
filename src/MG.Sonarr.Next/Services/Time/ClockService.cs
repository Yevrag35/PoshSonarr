using MG.Sonarr.Next.Services.Time.Mock;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Time
{
    file sealed class ClockService : IClock
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
        public DateTime Today => DateTime.Today;
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public ClockService() { }
    }

    public static class ClockServiceDepedencyInjection
    {
        public static IServiceCollection AddClock(this IServiceCollection services)
        {
            return services.AddSingleton<IClock, ClockService>();
        }

        public static IServiceCollection AddClock(this IServiceCollection services, Action<IMockClockParameters> setupCallbacks)
        {
            ClockService baseClock = new();
            MockClockParameters parameters = new();
            setupCallbacks(parameters);

            return services.AddSingleton<IClock>(new MockClockService(baseClock, parameters));
        }
    }
}
