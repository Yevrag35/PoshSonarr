using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.WantedMissing;

/// <summary>
/// Represents the <c>Get-SonarrWantedMissing</c> cmdlet, which gets the episodes that Sonarr wants and doesn't have
/// files for.
/// </summary>
/// <remarks>
/// <para>
/// Sonarr returns the missing episodes a page at a time, and the cmdlet writes each episode on the page to the
/// pipeline as an <see cref="EpisodeObject"/>. When a request fails, the cmdlet writes its error.
/// </para>
/// <para>
/// <c>-PageNumber</c> and <c>-PageSize</c>, the default parameter set, pick the page, and when they're left out,
/// Sonarr's own defaults apply. <c>-All</c> gets every missing episode instead: it reads how many there are with a
/// request for a page of one, and then gets them all on one page.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrWantedMissing", DefaultParameterSetName = "ByPage")]
public sealed class GetSonarrWantedMissingCmdlet : SonarrApiCmdletBase
{
	private QueryCol _parameters = null!;

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet gets every missing episode instead of one page.
	/// </summary>
	/// <remarks>
	/// The switch can't be combined with <c>-PageNumber</c> or <c>-PageSize</c>.
	/// </remarks>
	/// <value>The switch, which is present to get every missing episode.</value>
	[Parameter(Mandatory = true, ParameterSetName = "AllRecords")]
	public SwitchParameter All { get; set; }

	/// <summary>
	/// Gets or sets the number of the page to get.
	/// </summary>
	/// <remarks>
	/// The parameter's alias is <c>-Page</c>.
	/// </remarks>
	/// <value>
	/// The page's number, starting at 1. The default is 0, which sends no page number, so Sonarr's default applies.
	/// </value>
	[Parameter(Mandatory = false, ParameterSetName = "ByPage")]
	[Alias("Page")]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int PageNumber { get; set; }

	/// <summary>
	/// Gets or sets the number of episodes on each page.
	/// </summary>
	/// <value>
	/// The page's size, greater than zero. The default is 0, which sends no page size, so Sonarr's default applies.
	/// </value>
	[Parameter(Mandatory = false, ParameterSetName = "ByPage")]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int PageSize { get; set; }

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the query that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_parameters = new(2);
	}
	/// <summary>
	/// Adds <c>-PageNumber</c> and <c>-PageSize</c> to the query when they're given.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		if (this.HasParameter(this.PageNumber))
		{
			_parameters.Add(PagingConstants.PageNumber, this.PageNumber);
		}

		if (this.HasParameter(this.PageSize))
		{
			_parameters.Add(PagingConstants.PageSize, this.PageSize);
		}
	}

	/// <summary>
	/// Gets the page of missing episodes, or every missing episode under <c>-All</c>, and writes the episodes to the
	/// pipeline.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.All)
		{
			IEnumerable<EpisodeObject> records = this.SendAllRecords();
			this.WriteCollection(records);
			return;
		}

		string url = GetUrl(_parameters);
		if (this.GetWantedMissing(url, out RecordResult<EpisodeObject>? result))
		{
			this.WriteCollection(result.Records);
		}
	}

	/// <summary>
	/// Gets one page of missing episodes, and writes the request's error when it fails.
	/// </summary>
	/// <param name="url">The endpoint's path, with the page's query.</param>
	/// <param name="result">
	/// When the method returns <see langword="true"/>, the page; otherwise, <see langword="null"/>.
	/// </param>
	/// <returns><see langword="true"/> if the request succeeded; otherwise, <see langword="false"/>.</returns>
	private bool GetWantedMissing(string url, [NotNullWhen(true)] out RecordResult<EpisodeObject>? result)
	{
		result = null;
		var response = this.SendGetRequest<RecordResult<EpisodeObject>>(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return false;
		}

		result = response.Value;
		return true;
	}

	/// <summary>
	/// Gets the wanted/missing endpoint's path with the specified query.
	/// </summary>
	/// <remarks>
	/// The path is built in a buffer on the stack, sized for the endpoint's path, the query's separator, and the
	/// longest text that the query can format to.
	/// </remarks>
	/// <param name="parameters">The query's fields.</param>
	/// <returns>The endpoint's path, relative to the API, with the query when it holds a field.</returns>
	private static string GetUrl(QueryCol parameters)
	{
		if (parameters.Count == 0)
		{
			return Constants.WantedMissing;
		}

		Span<char> span = stackalloc char[Constants.WantedMissing.Length + 1 + parameters.MaxLength];
		Constants.WantedMissing.CopyTo(span);
		int position = Constants.WantedMissing.Length;

		_ = parameters.TryFormat(span.Slice(position), out int written);
		return new string(span.Slice(0, position + written));
	}

	/// <summary>
	/// Gets every missing episode: first a page of one, to read how many there are, and then one page that holds them
	/// all.
	/// </summary>
	/// <remarks>
	/// When either request fails, the method writes its error and returns an empty list.
	/// </remarks>
	/// <returns>Every missing episode, or an empty list when a request fails.</returns>
	private MetadataList<EpisodeObject> SendAllRecords()
	{
		_parameters.Add(PagingConstants.PageNumber, 1);
		_parameters.Add(PagingConstants.PageSize, 1);

		string url = GetUrl(_parameters);
		if (!this.GetWantedMissing(url, out RecordResult<EpisodeObject>? result))
		{
			return [];
		}

		_parameters.AddOrUpdate(PagingConstants.PageSize, result.TotalRecords);

		url = GetUrl(_parameters);
		return this.GetWantedMissing(url, out result)
			? result.Records
			: [];
	}
}
