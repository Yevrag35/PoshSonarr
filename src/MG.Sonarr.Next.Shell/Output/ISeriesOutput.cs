namespace MG.Sonarr.Next.Shell.Output;

/// <summary>
/// Represents the properties of the series that <c>Get-SonarrSeries</c> writes, for the cmdlet's
/// <see cref="OutputTypeAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// No model implements the interface. The cmdlet writes each series as a
/// <see cref="MG.Sonarr.Next.Models.Series.SeriesObject"/>, whose PowerShell properties come from the fields of Sonarr's
/// JSON for a series, and the interface names the ones that a series has. The <c>monitored</c>, <c>tvdbId</c>, and
/// <c>seasonFolder</c> fields become <see cref="IsMonitored"/>, <see cref="TVDbId"/>, and
/// <see cref="UseSeasonFolders"/>, and the <see cref="SeriesType"/> and <see cref="Status"/> values get an uppercase
/// first letter.
/// </para>
/// <para>
/// The <c>Id</c> property is the series' ID, and <c>Tags</c> holds the IDs of its tags.
/// </para>
/// </remarks>
internal interface ISeriesOutput : IIdTagOuptut
{
	/// <summary>
	/// Gets the time that the series was added to Sonarr's library.
	/// </summary>
	/// <value>The time that Sonarr added the series.</value>
	DateTimeOffset Added { get; }
	/// <summary>
	/// Gets the series' other titles.
	/// </summary>
	/// <value>The objects that hold the series' alternate titles, which can be empty.</value>
	object[] AlternateTitles { get; }
	/// <summary>
	/// Gets the series' title as Sonarr reduces it to match releases.
	/// </summary>
	/// <value>The title in lowercase, without spaces or punctuation.</value>
	string CleanTitle { get; }
	/// <summary>
	/// Gets a value that indicates whether the series has ended.
	/// </summary>
	/// <value><see langword="true"/> if the series has ended; otherwise, <see langword="false"/>.</value>
	bool Ended { get; }
	/// <summary>
	/// Gets the series' genres.
	/// </summary>
	/// <value>The names of the series' genres, which can be empty.</value>
	string[] Genres { get; }
	/// <summary>
	/// Gets the series' images.
	/// </summary>
	/// <value>The objects that describe the series' posters, banners, and other images.</value>
	object[] Images { get; }
	/// <summary>
	/// Gets a value that indicates whether Sonarr monitors the series.
	/// </summary>
	/// <value><see langword="true"/> if the series is monitored; otherwise, <see langword="false"/>.</value>
	bool IsMonitored { get; }
	/// <summary>
	/// Gets the ID of the series' language profile.
	/// </summary>
	/// <value>The language profile's ID.</value>
	int LanguageProfileId { get; }
	/// <summary>
	/// Gets the network that airs the series.
	/// </summary>
	/// <value>The network's name.</value>
	string Network { get; }
	/// <summary>
	/// Gets the series' overview.
	/// </summary>
	/// <value>The description of the series.</value>
	string Overview { get; }
	/// <summary>
	/// Gets the series' folder.
	/// </summary>
	/// <value>The full path of the folder that holds the series' files.</value>
	string Path { get; }
	/// <summary>
	/// Gets the ID of the series' quality profile.
	/// </summary>
	/// <value>The quality profile's ID.</value>
	int QualityProfileId { get; }
	/// <summary>
	/// Gets the series' ratings.
	/// </summary>
	/// <value>The object that holds the series' rating and the number of votes.</value>
	object Ratings { get; }
	/// <summary>
	/// Gets the root folder that holds the series' folder.
	/// </summary>
	/// <value>The root folder's path.</value>
	string RootFolderPath { get; }
	/// <summary>
	/// Gets the length of the series' episodes.
	/// </summary>
	/// <value>The episodes' run time, in minutes.</value>
	int Runtime { get; }
	/// <summary>
	/// Gets the series' seasons.
	/// </summary>
	/// <value>The objects that describe each season and whether Sonarr monitors it.</value>
	object[] Seasons { get; }
	/// <summary>
	/// Gets the kind of series, which decides how Sonarr numbers its episodes.
	/// </summary>
	/// <value>The series type, such as <c>Standard</c>, <c>Daily</c>, or <c>Anime</c>.</value>
	string SeriesType { get; }
	/// <summary>
	/// Gets the title that the series sorts by.
	/// </summary>
	/// <value>The series' sort title.</value>
	string SortTitle { get; }
	/// <summary>
	/// Gets the series' statistics.
	/// </summary>
	/// <value>The object that holds the series' counts of seasons, episodes, and files, and their size on disk.</value>
	object Statistics { get; }
	/// <summary>
	/// Gets the series' status.
	/// </summary>
	/// <value>The status, such as <c>Continuing</c> or <c>Ended</c>.</value>
	string Status { get; }
	/// <summary>
	/// Gets the series' title.
	/// </summary>
	/// <value>The title of the series.</value>
	string Title { get; }
	/// <summary>
	/// Gets the series' title as Sonarr writes it in a URL.
	/// </summary>
	/// <value>The series' title slug.</value>
	string TitleSlug { get; }
	/// <summary>
	/// Gets the series' TVDB ID.
	/// </summary>
	/// <value>The series' ID on TheTVDB.</value>
	int TVDbId { get; }
	/// <summary>
	/// Gets the series' TVmaze ID.
	/// </summary>
	/// <value>The series' ID on TVmaze, or 0 if it has none.</value>
	int TvMazeId { get; }
	/// <summary>
	/// Gets the series' TVRage ID.
	/// </summary>
	/// <value>The series' ID on TVRage, or 0 if it has none.</value>
	int TvRageId { get; }
	/// <summary>
	/// Gets a value that indicates whether Sonarr matches releases by the scene's numbering of the series.
	/// </summary>
	/// <value><see langword="true"/> if the series uses scene numbering; otherwise, <see langword="false"/>.</value>
	bool UseSceneNumbering { get; }
	/// <summary>
	/// Gets a value that indicates whether the series' files go in a folder for each season.
	/// </summary>
	/// <value><see langword="true"/> if the series uses season folders; otherwise, <see langword="false"/>.</value>
	bool UseSeasonFolders { get; }
	/// <summary>
	/// Gets the year that the series first aired.
	/// </summary>
	/// <value>The series' year.</value>
	int Year { get; }
}
