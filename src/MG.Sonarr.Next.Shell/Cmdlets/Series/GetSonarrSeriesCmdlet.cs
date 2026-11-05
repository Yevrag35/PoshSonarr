using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Output;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series;

/// <summary>
/// Represents the <c>Get-SonarrSeries</c> cmdlet, which gets the series in Sonarr's library by ID, by name, or for
/// the objects piped to it.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet writes each series to the pipeline as a <see cref="SeriesObject"/>. It gets a series that it has an ID
/// for with a request of its own, and every series with one request when it matches names or when it's given
/// nothing, in which case it writes the whole library.
/// </para>
/// <para>
/// <c>-Name</c>, the default parameter set, takes IDs and names together, and its names can hold wildcards.
/// <c>-Id</c> takes IDs only. A series, an episode, an episode file, a calendar entry, or a rename result piped to the
/// cmdlet gets the series that the object belongs to.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrSeries", DefaultParameterSetName = "BySeriesName")]
[MetadataCanPipe(Tag = Meta.Calendar)]
[MetadataCanPipe(Tag = Meta.Episode)]
[MetadataCanPipe(Tag = Meta.EpisodeFile)]
[MetadataCanPipe(Tag = Meta.Renamable)]
[OutputType(typeof(ISeriesOutput))]
public sealed class GetSonarrSeriesCmdlet : SonarrMetadataCmdlet
{
	static readonly string _namePropertyName = nameof(Name);
	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;

	/// <summary>
	/// Gets or sets the IDs and the names of the series to get.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Given by position, a number, or text that reads as a whole number, is a series' ID, which has to be greater
	/// than zero, and any other text is a name. Given by name, as <c>-Name</c>, every value is a name, a number
	/// included. A name can hold wildcards and matches a series' title ignoring case.
	/// </para>
	/// <para>
	/// When the parameter is left out, the cmdlet gets every series.
	/// </para>
	/// </remarks>
	/// <value>The IDs and names of the series. The default is an empty array, for every series.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesName")]
	[SupportsWildcards, ValidateIds(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
	public Either<string, int>[] Name { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs of the series to get.
	/// </summary>
	/// <remarks>
	/// The cmdlet gets each series with a request of its own, and writes nothing for an ID that Sonarr doesn't have.
	/// </remarks>
	/// <value>The series' IDs, each greater than zero. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the objects whose series the cmdlet gets.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameter takes pipeline input, and each piped object arrives in an array of its own. It takes any object
	/// that implements <see cref="ISeriesPipeable"/>, such as a series, an episode, an episode file, a calendar entry,
	/// or a rename result, and the cmdlet gets the series whose ID the object gives. Each object's series ID has to be
	/// greater than zero.
	/// </para>
	/// <para>
	/// The parameter is hidden from tab completion, since it's meant for the pipeline.
	/// </para>
	/// </remarks>
	/// <value>The piped objects. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetPipeline, DontShow = true,
		ValueFromPipeline = true)]
	[ValidateIds<ISeriesPipeable>(ValidateRangeKind.Positive)]
	public ISeriesPipeable[] InputObject { get; set; } = [];

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the sets of IDs and names that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
		_wcNames = new();
	}
	/// <summary>
	/// Gets the metadata tag of the series endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The series' metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.Series];
	}

	/// <summary>
	/// Adds the IDs from <c>-Id</c> to the set of IDs that the cmdlet gets.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	/// <summary>
	/// Gets the series for the IDs, the names, or the piped objects, and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The cmdlet gets each series that it has an ID for with a request of its own, and writes the error of a request
	/// that fails, unless the error is ignorable, as it is for an ID that Sonarr doesn't have. Then, when it has names,
	/// it gets every series and writes those whose titles match one of the names, and when it has neither IDs nor
	/// names, it writes every series.
	/// </para>
	/// <para>
	/// The IDs and names serve only the record that gave them, so each piped object writes its own series and none
	/// that an earlier object gave.
	/// </para>
	/// <para>
	/// When the request for every series fails, the cmdlet stops with its error.
	/// </para>
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length > 0)
		{
			_ids.AddRange(this.InputObject);
		}
		else
		{
			this.Name.SplitToSets(_ids, _wcNames, !this.MyInvocation.IsBoundPositionally(_namePropertyName));
		}

		bool hadIds = false;
		if (_ids.Count > 0)
		{
			hadIds = true;
			this.WriteSeriesById(_ids);
			_ids.Clear();
		}

		if (_wcNames.Count > 0)
		{
			var response = this.GetSeriesByName(_wcNames);
			_wcNames.Clear();
			if (response.IsError)
			{
				this.StopCmdlet(response.Error);
				return;
			}

			this.WriteCollection(response.Value);
		}
		else if (!hadIds)
		{
			var response = this.GetAllSeries<SeriesObject>();
			if (response.IsError)
			{
				this.StopCmdlet(response.Error);
				return;
			}

			this.WriteCollection(response.Value);
		}
	}

	/// <summary>
	/// Gets every series and removes from the list those whose titles match none of the specified names.
	/// </summary>
	/// <remarks>
	/// A series without a title is removed too.
	/// </remarks>
	/// <param name="names">The names, which can hold wildcards, that the series' titles are matched against.</param>
	/// <returns>
	/// The result of the request, whose list holds the series that match, or the request's error when it fails.
	/// </returns>
	private SonarrClientResult<MetadataList<SeriesObject>> GetSeriesByName(WildcardSet names)
	{
		var result = this.GetAllSeries<SeriesObject>();
		if (result.IsError)
		{
			return result;
		}

		for (int i = result.Value.Count - 1; i >= 0; i--)
		{
			PSObject item = result.Value[i];
			if (!item.TryGetProperty(Constants.Title, out string? title)
				||
				title is null
				||
				!names.IsAnyMatch(title))
			{
				result.Value.RemoveAt(i);
			}
		}

		return result;
	}
	/// <summary>
	/// Gets every series in the library with one request.
	/// </summary>
	/// <typeparam name="T">The type that each series is read as.</typeparam>
	/// <returns>The result of the request, whose list holds every series, sorted, or the request's error.</returns>
	private SonarrClientResult<MetadataList<T>> GetAllSeries<T>()
		where T : PSObject, IComparable<T>, IJsonMetadataTaggable
	{
		return this.SendGetRequest<MetadataList<T>>(this.Tag.UrlBase);
	}
	/// <summary>
	/// Gets the series with each of the specified IDs, with a request for each one, and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// When a request fails, the method writes its error, unless the error is ignorable, as it is for an ID that Sonarr
	/// doesn't have, and goes on to the next ID.
	/// </remarks>
	/// <param name="ids">The IDs of the series to get, in the order that the requests go out.</param>
	private void WriteSeriesById(SortedSet<int> ids)
	{
		foreach (int id in ids)
		{
			SonarrClientResult<SeriesObject> result = this.SendGetRequest<SeriesObject>(this.Tag.GetUrlForId(id));
			if (result.IsError)
			{
				this.WriteConditionalError(result.Error);
				continue;
			}

			this.WriteObject(result.Value);
		}
	}
}
