namespace MG.Sonarr.Next.Services.Time.Mock
{
    internal sealed class MockClockService : IClock
    {
        readonly IClock _baseClock;
        readonly MockClockParameters _parameters;

        public DateTimeOffset Now => _parameters.GetNow(_baseClock);
        public DateTime Today => _parameters.GetToday(_baseClock);
        public DateTimeOffset UtcNow => _parameters.GetUtcNow(_baseClock);

        internal MockClockService(IClock baseClock, MockClockParameters parameters)
        {
            ArgumentNullException.ThrowIfNull(baseClock);
            ArgumentNullException.ThrowIfNull(parameters);

            SetParametersIfNull(parameters);

            _baseClock = baseClock;
            _parameters = parameters;
        }

        private static DateTimeOffset GetDefaultGetNow(IClock baseClock)
        {
            return baseClock.Now;
        }
        private static DateTime GetDefaultToday(IClock baseClock)
        {
            return baseClock.Today;
        }
        private static DateTimeOffset GetDefaultUtcNow(IClock baseClock)
        {
            return baseClock.UtcNow;
        }
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
}
