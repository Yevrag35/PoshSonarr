using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models;

namespace MG.Sonarr.Next.Shell.Output
{
    internal interface ICalendarOutput : IHasId, IJsonSonarrMetadata
    {
        int AbsoluteEpisodeNumber { get; }
        DateTimeOffset AirDateUtc { get; }
        int EpisodeNumber { get; }
        int EpisodeFileId { get; }
        bool HasFile { get; }
        bool IsMonitored { get; }
        int SceneAbsoluteEpisodeNumber { get; }
        int SceneEpisodeNumber { get; }
        int SceneSeasonNumber { get; }
        int SeasonNumber { get; }
        int SeriesId { get; }
        string Title { get; }
        int TVDbId { get; }
        bool UnverifiedSceneNumbering { get; }
    }
}
