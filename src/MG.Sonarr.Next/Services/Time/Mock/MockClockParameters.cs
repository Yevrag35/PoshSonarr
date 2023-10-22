namespace MG.Sonarr.Next.Services.Time.Mock
{
    public interface IMockClockParameters
    {
        Func<IClock, DateTimeOffset> GetNow { set; }
        Func<IClock, DateTime> GetToday { set; }
        Func<IClock, DateTimeOffset> GetUtcNow { set; }
    }

    internal sealed class MockClockParameters : IMockClockParameters
    {
        public Func<IClock, DateTimeOffset> GetNow { get; set; } = null!;
        public Func<IClock, DateTime> GetToday { get; set; } = null!;
        public Func<IClock, DateTimeOffset> GetUtcNow { get; set; } = null!;
    }
}
