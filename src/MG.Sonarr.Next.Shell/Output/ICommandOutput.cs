using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models;

namespace MG.Sonarr.Next.Shell.Output
{
    internal interface ICommandOutput : IHasId, IJsonSonarrMetadata
    {
        string ClientUserAgent { get; }
        string CommandName { get; }
        TimeSpan? Duration { get; }
        DateTimeOffset? Ended { get; }
        string Message { get; }
        string Priority { get; }
        DateTimeOffset Queued { get; }
        bool SendUpdatesToClient { get; }
        DateTimeOffset Started { get; }
        DateTimeOffset? StateChangeTime { get; }
        string Trigger { get; }
        bool UpdateScheduledTask { get; }
    }
}
