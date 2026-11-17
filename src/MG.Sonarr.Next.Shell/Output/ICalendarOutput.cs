using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models;

namespace MG.Sonarr.Next.Shell.Output;

/// <summary>
/// Represents the properties of the calendar entries that <c>Get-SonarrCalendar</c> writes, for the cmdlet's
/// <see cref="OutputTypeAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// No model implements the interface. The cmdlet writes each entry as a
/// <see cref="MG.Sonarr.Next.Models.Calendar.CalendarObject"/>, whose PowerShell properties come from the fields of
/// Sonarr's JSON for an episode, and the interface names the ones that an entry has. The <c>monitored</c> and
/// <c>tvdbId</c> fields become <see cref="IsMonitored"/> and <see cref="TVDbId"/>.
/// </para>
/// <para>
/// The <c>Id</c> property is the episode's ID, and the <c>MetadataTag</c> property is the calendar's tag.
/// </para>
/// </remarks>
internal interface ICalendarOutput : IHasId, IJsonSonarrMetadata
{
	/// <summary>
	/// Gets the episode's absolute number in its series.
	/// </summary>
	/// <value>The episode's absolute number, which Sonarr leaves out when the episode has none.</value>
	int AbsoluteEpisodeNumber { get; }
	/// <summary>
	/// Gets the time that the episode airs, in UTC.
	/// </summary>
	/// <value>The episode's air time.</value>
	DateTimeOffset AirDateUtc { get; }
	/// <summary>
	/// Gets the episode's number in its season.
	/// </summary>
	/// <value>The episode number.</value>
	int EpisodeNumber { get; }
	/// <summary>
	/// Gets the ID of the episode's file.
	/// </summary>
	/// <value>The ID of the episode's file, or 0 if the episode has none.</value>
	int EpisodeFileId { get; }
	/// <summary>
	/// Gets a value that indicates whether the episode has a file.
	/// </summary>
	/// <value><see langword="true"/> if the episode has a file; otherwise, <see langword="false"/>.</value>
	bool HasFile { get; }
	/// <summary>
	/// Gets a value that indicates whether Sonarr monitors the episode.
	/// </summary>
	/// <value><see langword="true"/> if the episode is monitored; otherwise, <see langword="false"/>.</value>
	bool IsMonitored { get; }
	/// <summary>
	/// Gets the absolute episode number that scene releases use for the episode.
	/// </summary>
	/// <value>The scene's absolute episode number.</value>
	int SceneAbsoluteEpisodeNumber { get; }
	/// <summary>
	/// Gets the episode number that scene releases use for the episode.
	/// </summary>
	/// <value>The scene's episode number.</value>
	int SceneEpisodeNumber { get; }
	/// <summary>
	/// Gets the season number that scene releases use for the episode.
	/// </summary>
	/// <value>The scene's season number.</value>
	int SceneSeasonNumber { get; }
	/// <summary>
	/// Gets the number of the episode's season.
	/// </summary>
	/// <value>The season number.</value>
	int SeasonNumber { get; }
	/// <summary>
	/// Gets the ID of the episode's series.
	/// </summary>
	/// <value>The series' ID.</value>
	int SeriesId { get; }
	/// <summary>
	/// Gets the episode's title.
	/// </summary>
	/// <value>The title of the episode.</value>
	string Title { get; }
	/// <summary>
	/// Gets the episode's TVDB ID.
	/// </summary>
	/// <value>The episode's ID on TheTVDB.</value>
	int TVDbId { get; }
	/// <summary>
	/// Gets a value that indicates whether the episode's scene numbering hasn't been verified.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the scene numbering is unverified; otherwise, <see langword="false"/>.
	/// </value>
	bool UnverifiedSceneNumbering { get; }
}
