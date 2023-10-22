namespace MG.Sonarr.Next.Services.Time
{
    public interface IClock
    {
        DateTimeOffset Now { get; }
        DateTime Today { get; }
        DateTimeOffset UtcNow { get; }
    }
}
