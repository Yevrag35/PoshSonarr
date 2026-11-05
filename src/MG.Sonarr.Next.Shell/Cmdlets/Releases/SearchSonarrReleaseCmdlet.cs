using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Releases;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Releases;

/// <summary>
/// Represents the <c>Search-SonarrRelease</c> cmdlet, which has Sonarr search its indexers for the releases of an
/// episode or of a series' season.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet sends one search request for each episode or season, and writes the releases that Sonarr finds to the
/// pipeline as <see cref="ReleaseObject"/> objects, which can be piped to <c>Add-SonarrRelease</c>. When a request
/// fails, the cmdlet writes its error and goes on to the next input.
/// </para>
/// <para>
/// <c>-EpisodeId</c>, the default parameter set, searches for one episode, and an episode piped to the cmdlet
/// searches for that episode. <c>-SeriesId</c> with <c>-SeasonNumber</c> searches for a season of a series, and a
/// series piped to the cmdlet, with <c>-SeasonNumber</c>, searches for that season of the series.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Search, "SonarrRelease", DefaultParameterSetName = ByEpisodeId)]
[MetadataCanPipe(Tag = Meta.Episode)]
[MetadataCanPipe(Tag = Meta.Series)]
public sealed class SearchSonarrReleaseCmdlet : SonarrApiCmdletBase
{
	private const string ByEpisodeId = "ByEpisodeId";
	private const string ByEpisodeInput = "ByEpisodeInput";
	private const string BySeriesId = "BySeriesId";
	private const string BySeriesInput = "BySeriesInput";
	private static readonly Wildcard _byEpisodeWildcard = Wildcard
		.ParseAs(WildcardMatchType.StartsWith, ByEpisodeId.AsSpan(0, ByEpisodeId.Length - 2));

	QueryCol _queryParams = null!;
	/// <summary>
	/// Gets or sets the metadata tag of the release endpoint, which the cmdlet sends its searches to.
	/// </summary>
	/// <value>The <c>#release</c> tag, set when the cmdlet's scope is created.</value>
	MetadataTag Tag { get; set; } = null!;

	/// <summary>
	/// Gets or sets the ID of the episode to search for.
	/// </summary>
	/// <remarks>
	/// An episode piped to the cmdlet sets this value to its own ID.
	/// </remarks>
	/// <value>The episode's ID, greater than zero. The default is 0.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = ByEpisodeId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int EpisodeId { get; set; }

	/// <summary>
	/// Gets or sets the episode to search for.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameter takes pipeline input, one episode at a time. It takes any object that implements
	/// <see cref="IReleasePipeableByEpisode"/>, such as an episode, whose ID has to be greater than zero.
	/// </para>
	/// <para>
	/// The episode's ID is kept as <see cref="EpisodeId"/>, and a read gives <see langword="null"/>.
	/// </para>
	/// </remarks>
	/// <value>The episode, which a read gives as <see langword="null"/>.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ByEpisodeInput)]
	[ValidateId<IReleasePipeableByEpisode>(ValidateRangeKind.Positive)]
	public IReleasePipeableByEpisode Episode
	{
		get => null!;
		set => this.EpisodeId = value?.EpisodeId ?? 0;
	}

	/// <summary>
	/// Gets or sets the ID of the series whose season the cmdlet searches for.
	/// </summary>
	/// <remarks>
	/// A series piped to the cmdlet sets this value to its own ID.
	/// </remarks>
	/// <value>The series' ID, greater than zero. The default is 0.</value>
	[Parameter(Mandatory = true, ParameterSetName = BySeriesId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int SeriesId { get; set; }

	/// <summary>
	/// Gets or sets the series whose season the cmdlet searches for.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameter takes pipeline input, one series at a time. It takes any object that implements
	/// <see cref="IReleasePipeableBySeries"/>, such as a series, whose ID has to be greater than zero.
	/// </para>
	/// <para>
	/// The series' ID is kept as <see cref="SeriesId"/>, and a read gives <see langword="null"/>.
	/// </para>
	/// </remarks>
	/// <value>The series, which a read gives as <see langword="null"/>.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BySeriesInput)]
	[ValidateId<IReleasePipeableBySeries>(ValidateRangeKind.Positive)]
	public IReleasePipeableBySeries Series
	{
		get => null!;
		set => this.SeriesId = value?.SeriesId ?? 0;
	}

	/// <summary>
	/// Gets or sets the number of the season to search for.
	/// </summary>
	/// <remarks>
	/// The parameter goes with <c>-SeriesId</c> and with a piped series. Its alias is <c>-Season</c>.
	/// </remarks>
	/// <value>The season's number, where 0 is the specials. The default is 0.</value>
	[Parameter(Mandatory = true, ParameterSetName = BySeriesId)]
	[Parameter(Mandatory = true, ParameterSetName = BySeriesInput)]
	[Alias("Season")]
	[ValidateRange(ValidateRangeKind.NonNegative)]
	public int SeasonNumber { get; set; }

	/// <summary>
	/// Runs the base class's setup for the scope, then gets the metadata tag of the release endpoint and creates the
	/// query that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.Release];
		_queryParams = new(10);
	}

	/// <summary>
	/// Searches for the releases of the episode or the season that the cmdlet holds, and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// The query is rebuilt for each input. When the cmdlet holds neither an episode ID nor a series ID, it sends no
	/// request, and when the request fails, it writes the error, whatever it is.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		_queryParams.Clear();

		if (this.EpisodeId == 0 && this.SeriesId == 0)
		{
			return;
		}

		string url = this.GetUrl();

		var response = this.SendGetRequest<MetadataList<ReleaseObject>>(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteCollection(response.Value);
	}

	/// <summary>
	/// Adds the episode ID, in the episode parameter sets, or else the series ID and the season number, to the query,
	/// and gets the release endpoint's path with it.
	/// </summary>
	/// <returns>The release endpoint's path, relative to the API, with the query.</returns>
	private string GetUrl()
	{
		if (this.ParameterSetNameIsLike(_byEpisodeWildcard))
		{
			_queryParams.Add(Constants.EpisodeId, this.EpisodeId);
		}
		else
		{
			_queryParams.Add(Constants.SeriesIdLowercase, this.SeriesId);
			_queryParams.Add(Constants.SeasonNumber, this.SeasonNumber);
		}

		return this.Tag.GetUrl(_queryParams);
	}
}
