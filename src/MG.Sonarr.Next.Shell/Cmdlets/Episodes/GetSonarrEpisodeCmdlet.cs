using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes;

/// <summary>
/// Represents the <c>Get-SonarrEpisode</c> cmdlet, which gets episodes by their IDs or by the series that they belong
/// to.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet collects its IDs until its input ends, and then writes each episode to the pipeline as an
/// <see cref="EpisodeObject"/>. It gets an episode by its ID with a request of its own, and a series' episodes with
/// one request for each series.
/// </para>
/// <para>
/// <c>-Id</c>, the default parameter set, takes episode IDs, and a calendar entry piped to the cmdlet gets the
/// entry's episode. <c>-SeriesId</c> takes series IDs, and a series piped to the cmdlet gets the series' episodes,
/// which name the series by its title. With a series, <c>-EpisodeIdentifier</c> keeps only the episodes that it
/// names.
/// </para>
/// <para>
/// When a request fails, the cmdlet writes its error, unless the error is ignorable, as it is for an ID that Sonarr
/// doesn't have, and goes on to the next ID.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrEpisode", DefaultParameterSetName = BY_EP_ID)]
[MetadataCanPipe(Tag = Meta.Calendar)]
[MetadataCanPipe(Tag = Meta.Series)]
public sealed class GetSonarrEpisodeCmdlet : SonarrMetadataCmdlet
{
	const string BY_EP_ID = "ByEpisodeId";
	const string BY_EP_INPUT = "ByEpisodeInput";
	const string BY_SERIES_ID = "BySeriesId";
	const string BY_SERIES_INPUT = "BySeriesInput";

	SortedSet<int> _epIds = null!;
	QueryCol _params = null!;
	Dictionary<int, IEpisodeBySeriesPipeable> _seriesIds = null!;

	/// <summary>
	/// Gets or sets the IDs of the episodes to get.
	/// </summary>
	/// <value>The episodes' IDs, each greater than zero. The default is an empty array.</value>
	[Parameter(Mandatory = true, ParameterSetName = BY_EP_ID)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs of the series whose episodes the cmdlet gets.
	/// </summary>
	/// <value>The series' IDs, each greater than zero. The default is an empty array.</value>
	[Parameter(Mandatory = true, ParameterSetName = BY_SERIES_ID)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] SeriesId { get; set; } = [];

	/// <summary>
	/// Gets or sets the objects whose episodes the cmdlet gets.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and each piped object arrives in an array of its own. It takes any object
	/// that implements <see cref="IEpisodePipeable"/>, such as a calendar entry, and the cmdlet gets the episode whose
	/// ID the object gives, which has to be greater than zero.
	/// </remarks>
	/// <value>The piped objects. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_EP_INPUT)]
	[ValidateIds<IEpisodePipeable>(ValidateRangeKind.Positive)]
	public IEpisodePipeable[] EpisodeInput { get; set; } = [];

	/// <summary>
	/// Gets or sets the series whose episodes the cmdlet gets.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and each piped series arrives in an array of its own. It takes any object
	/// that implements <see cref="IEpisodeBySeriesPipeable"/>, such as a series, whose ID has to be greater than zero.
	/// Each episode that the cmdlet writes names its series by the title of the series that was piped.
	/// </remarks>
	/// <value>The piped series. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_SERIES_INPUT)]
	[ValidateIds<IEpisodeBySeriesPipeable>(ValidateRangeKind.Positive)]
	public IEpisodeBySeriesPipeable[] SeriesInput { get; set; } = [];

	/// <summary>
	/// Gets or sets the identifiers of the series' episodes to keep.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Each identifier is a season and an episode, such as <c>S01E02</c>, a season and a range of episodes, such as
	/// <c>S01E02-05</c>, or an absolute episode number, such as <c>12</c>. The cmdlet writes the episodes that match
	/// any of them.
	/// </para>
	/// <para>
	/// The parameter goes with <c>-SeriesId</c>, at position 1, and with piped series, at position 0. When an
	/// identifier isn't valid, the cmdlet stops with an error before it sends a request. The parameter's alias is
	/// <c>-SeasonEpId</c>.
	/// </para>
	/// </remarks>
	/// <value>The identifiers, or <see langword="null"/> or empty for every episode.</value>
	[Parameter(Mandatory = false, Position = 1, ParameterSetName = BY_SERIES_ID)]
	[Parameter(Mandatory = false, Position = 0, ParameterSetName = BY_SERIES_INPUT)]
	[Alias("SeasonEpId")]
	public SeasonEpisodeId[]? EpisodeIdentifier { get; set; }

	/// <summary>
	/// Gets the metadata tag of the episode endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The episode's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.Episode];
	}
	/// <summary>
	/// Runs the base class's setup for the scope, then creates the collections of episode and series IDs and the query
	/// that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_epIds = [];
		_seriesIds = [];
		_params = new(10);
	}
	/// <summary>
	/// Checks the identifiers in <c>-EpisodeIdentifier</c>, and stops the cmdlet with an error that names the invalid
	/// ones when any isn't valid.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Begin(IServiceProvider provider)
	{
		if (this.HasNotNullParameter(this.EpisodeIdentifier) && this.EpisodeIdentifier.Length > 0 && !this.EpisodeIdentifier.AreAllValid(out ErrorRecord? error))
		{
			this.StopCmdlet(error);
		}
	}

	/// <summary>
	/// Adds the episode or series IDs that the parameter set takes, from the parameters or the piped objects, to those
	/// that the cmdlet gets.
	/// </summary>
	/// <remarks>
	/// A series is kept once, as the first object that gave its ID.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		switch (this.ParameterSetName)
		{
			case BY_EP_ID:
				_epIds.UnionWith(this.Id);
				break;

			case BY_SERIES_ID:
				AddSeriesIds(this.SeriesId, _seriesIds);
				break;

			case BY_EP_INPUT:
				_epIds.UnionWith(this.EpisodeInput.Select(x => x.EpisodeId));
				break;

			case BY_SERIES_INPUT:
				AddSeries(this.SeriesInput, _seriesIds);
				break;
		}
	}
	/// <summary>
	/// Gets the episodes for the collected episode or series IDs, keeps those that <c>-EpisodeIdentifier</c> names
	/// when it's given, and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// When no ID was collected, the cmdlet sends no request.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void End(IServiceProvider provider)
	{
		if (_epIds.IsNullOrEmpty && _seriesIds.IsNullOrEmpty)
		{
			return;
		}

		IEnumerable<EpisodeObject> episodes = this.ParameterSetNameIsLike("ByEpisode*")
			? this.GetEpisodesById<EpisodeObject>(_epIds)
			: this.GetEpisodesBySeries(_seriesIds);

		if (this.HasNotNullParameter(EpisodeIdentifier) && this.EpisodeIdentifier.Length > 0)
		{
			episodes = this.EpisodeIdentifier.FilterEpisodes(episodes);
		}

		this.WriteCollection(episodes);
	}

	/// <summary>
	/// Adds each of the specified series to the specified dictionary by its ID, unless the dictionary already holds the
	/// ID.
	/// </summary>
	/// <param name="input">The piped series.</param>
	/// <param name="dict">The series whose episodes the cmdlet gets, by their IDs.</param>
	private static void AddSeries(IEpisodeBySeriesPipeable[] input, IDictionary<int, IEpisodeBySeriesPipeable> dict)
	{
		foreach (IEpisodeBySeriesPipeable series in input)
		{
			_ = dict.TryAdd(series.SeriesId, series);
		}
	}
	/// <summary>
	/// Adds each of the specified series IDs to the specified dictionary, with <see cref="EmptySeries.Default"/> in
	/// place of a series, unless the dictionary already holds the ID.
	/// </summary>
	/// <param name="ids">The series' IDs.</param>
	/// <param name="dict">The series whose episodes the cmdlet gets, by their IDs.</param>
	private static void AddSeriesIds(int[] ids, IDictionary<int, IEpisodeBySeriesPipeable> dict)
	{
		foreach (int id in ids)
		{
			_ = dict.TryAdd(id, EmptySeries.Default);
		}
	}

	/// <summary>
	/// Gets the episodes with the specified IDs, with a request for each one.
	/// </summary>
	/// <remarks>
	/// The method is an iterator, so each request goes out as the sequence is enumerated. When a request fails, the
	/// method writes its error, unless the error is ignorable, and goes on to the next ID.
	/// </remarks>
	/// <typeparam name="T">The type that each episode is read as.</typeparam>
	/// <param name="episodeIds">The IDs of the episodes to get.</param>
	/// <returns>The episodes that were found, in the order of their IDs.</returns>
	private IEnumerable<T> GetEpisodesById<T>(IEnumerable<int> episodeIds) where T : PSObject
	{
		foreach (int id in episodeIds)
		{
			string url = this.Tag.GetUrlForId(id);
			var response = this.SendGetRequest<T>(url);
			if (response.IsError)
			{
				this.WriteConditionalError(response.Error);
			}
			else
			{
				yield return response.Value;
			}
		}
	}
	/// <summary>
	/// Gets the episodes of each of the specified series, with a request for each series, and gives each episode the
	/// series that it was found for.
	/// </summary>
	/// <remarks>
	/// Each request carries the series' ID as its <c>seriesId</c> query parameter, and the query is cleared for the
	/// next series. When a request fails, the method writes its error, unless the error is ignorable, and goes on to
	/// the next series. An episode of a piped series takes the series' title as the name of its series, and an episode
	/// of a series from <c>-SeriesId</c> keeps the name that it came with.
	/// </remarks>
	/// <param name="series">The series whose episodes to get, by their IDs.</param>
	/// <returns>The episodes of every series, with each series' episodes sorted.</returns>
	private List<EpisodeObject> GetEpisodesBySeries(Dictionary<int, IEpisodeBySeriesPipeable> series)
	{
		List<EpisodeObject> list = new(series.Count);
		foreach (int id in series.Keys)
		{
			_params.Add(Constants.SeriesIdLowercase, id);
			string url = this.Tag.GetUrl(_params);

			var response = this.SendGetRequest<MetadataList<EpisodeObject>>(url);
			if (response.IsError)
			{
				this.WriteConditionalError(response.Error);
				continue;
			}

			foreach (EpisodeObject obj in response.Value)
			{
				if (_seriesIds.TryGetValue(obj.SeriesId, out IEpisodeBySeriesPipeable? s))
				{
					obj.SetSeries(s);
				}

				list.Add(obj);
			}

			_params.Clear();
		}

		return list;
	}

	/// <summary>
	/// Represents the stand-in for a series that <c>-SeriesId</c> named by its ID alone, which gives its episodes no
	/// series of its own.
	/// </summary>
	/// <remarks>
	/// Its ID is 0 and its title is empty, so an episode that gets it keeps the name of its series that it already
	/// had.
	/// </remarks>
	private readonly struct EmptySeries : IEpisodeBySeriesPipeable
	{
		/// <summary>
		/// Gets the ID of the series.
		/// </summary>
		/// <value>Always 0.</value>
		public int SeriesId => 0;
		/// <summary>
		/// Gets the title of the series.
		/// </summary>
		/// <value>Always an empty string.</value>
		public string Title => string.Empty;

		/// <summary>
		/// Gets the metadata tag of the series.
		/// </summary>
		/// <value>Always <see cref="MetadataTag.Empty"/>.</value>
		public MetadataTag MetadataTag => MetadataTag.Empty;

		/// <summary>
		/// Does nothing, since the stand-in stands for no endpoint.
		/// </summary>
		/// <param name="resolver">The resolver, which the method doesn't use.</param>
		public void SetTag(IMetadataResolver resolver)
		{
			return;
		}

		/// <summary>
		/// Gets the ID that a cmdlet taking the stand-in as an <see cref="IEpisodeBySeriesPipeable"/> acts on.
		/// </summary>
		/// <returns>Always <see langword="null"/>, since the stand-in has no ID.</returns>
		int? IPipeable<IEpisodeBySeriesPipeable>.GetId()
		{
			return null;
		}

		/// <summary>
		/// The shared stand-in.
		/// </summary>
		internal static readonly EmptySeries Default = default;
	}
}
