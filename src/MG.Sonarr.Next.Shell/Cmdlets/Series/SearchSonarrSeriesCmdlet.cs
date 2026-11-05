using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Extensions;
using System.Net;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series;

/// <summary>
/// Represents the <c>Search-SonarrSeries</c> cmdlet, which looks up series by name or by TVDB ID through Sonarr, to
/// find series to add to the library.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet sends one lookup request to Sonarr and writes each result to the pipeline as an
/// <see cref="AddSeriesObject"/>, in the order that Sonarr ranks them. The results can be piped to
/// <c>Add-SonarrSeries</c>. When the request fails, the cmdlet stops with its error.
/// </para>
/// <para>
/// <c>-Name</c>, the default parameter set, searches by a series' name, and <c>-Strict</c> keeps only the results
/// whose titles fit the name. <c>-TVDbId</c> looks up the one series with that TVDB ID.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Search, "SonarrSeries", DefaultParameterSetName = "BySeriesName")]
public sealed class SearchSonarrSeriesCmdlet : SonarrApiCmdletBase
{
	static readonly string SEARCH_STR_QUERY = Constants.Series + "/lookup?term={0}";
	static readonly string SEARCH_ID_QUERY = Constants.Series + "/lookup?term=tvdb:{0}";
	Wildcard _wildcardStr;
	IMetadataResolver _resolver = null!;

	/// <summary>
	/// Gets or sets the name to search for.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The name can hold wildcards. Sonarr's lookup doesn't take them, so the cmdlet sends the name with each
	/// <c>*</c> and <c>?</c> turned into a space, and <c>-Strict</c> matches the results' titles against the name
	/// with its wildcards.
	/// </para>
	/// <para>
	/// The value is kept for the search, and a read gives an empty string.
	/// </para>
	/// </remarks>
	/// <value>The name, which a read gives as an empty string.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesName")]
	public string Name
	{
		get => string.Empty;
		set => _wildcardStr = value;
	}

	/// <summary>
	/// Gets or sets the TVDB ID of the series to look up.
	/// </summary>
	/// <value>The series' ID on TheTVDB. The default is 0.</value>
	[Parameter(Mandatory = true, ParameterSetName = "ByTVDbId")]
	public long TVDbId { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet writes only the results whose titles fit <c>-Name</c>.
	/// </summary>
	/// <remarks>
	/// A title fits when it matches the name as a wildcard pattern, or when it contains the name's text, both ignoring
	/// case.
	/// </remarks>
	/// <value>The switch, which is present to filter the results by their titles.</value>
	[Parameter(Mandatory = false, ParameterSetName = "BySeriesName")]
	public SwitchParameter Strict { get; set; }

	/// <summary>
	/// Runs the base class's setup for the scope, then gets the scope's metadata resolver, which tags the results.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_resolver = provider.GetRequiredService<IMetadataResolver>();
	}
	/// <summary>
	/// Sends the lookup request for <c>-Name</c> or <c>-TVDbId</c>, tags each result, and writes the results to the
	/// pipeline, or only those that fit <c>-Name</c> under <c>-Strict</c>.
	/// </summary>
	/// <remarks>
	/// When the request fails, the cmdlet stops with its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		string path = this.HasParameter(Name)
			? GetSearchByNamePath(_wildcardStr)
			: string.Format(CultureInfo.InvariantCulture, SEARCH_ID_QUERY, this.TVDbId);

		var result = this.SendGetRequest<List<AddSeriesObject>>(path);
		if (result.IsError)
		{
			this.StopCmdlet(result.Error);
			return;
		}

		// A list isn't tagged by the client, which keeps the order that Sonarr ranks its results in, so each result is
		// tagged here, with the #add_series tag that AddSeriesObject.GetTag returns.
		foreach (AddSeriesObject series in result.Value)
		{
			series.SetTag(_resolver);
		}

		if (this.Strict)
		{
			this.ProcessStricly(result.Value, in _wildcardStr);
		}
		else
		{
			this.WriteCollection(result.Value);
		}
	}

	/// <summary>
	/// Writes to the pipeline the specified results whose titles fit the specified name.
	/// </summary>
	/// <param name="values">The lookup results, in the order that Sonarr ranks them.</param>
	/// <param name="Wildcard">The name from <c>-Name</c>, which can hold wildcards.</param>
	private void ProcessStricly(IEnumerable<AddSeriesObject> values, in Wildcard Wildcard)
	{
		foreach (AddSeriesObject pso in values)
		{
			if (StrictlyMatches(pso.Title, in Wildcard))
			{
				this.WriteObject(pso);
			}
		}
	}

	/// <summary>
	/// Gets the lookup endpoint's path for the specified name, with each wildcard character turned into a space and the
	/// name URL-encoded.
	/// </summary>
	/// <param name="name">The name to search for, which can hold wildcards.</param>
	/// <returns>The lookup endpoint's path, relative to the API, with the name as its <c>term</c>.</returns>
	private static string GetSearchByNamePath(in Wildcard name)
	{
		string use = name.ContainsWildcards
			? ((string)name).Replace('*', ' ').Replace('?', ' ').Trim()
			: (string)name;

		return string.Format(CultureInfo.InvariantCulture, SEARCH_STR_QUERY, WebUtility.UrlEncode(use));
	}

	/// <summary>
	/// Determines whether the specified title matches the specified pattern, or contains the pattern's text, ignoring
	/// case.
	/// </summary>
	/// <param name="value">The title of a lookup result.</param>
	/// <param name="stringToMatch">The name from <c>-Name</c>, which can hold wildcards.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> matches or contains <paramref name="stringToMatch"/>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool StrictlyMatches(string value, in Wildcard stringToMatch)
	{
		return stringToMatch.IsMatch(value)
			   ||
			   value.Contains((string)stringToMatch, StringComparison.OrdinalIgnoreCase);
	}
}
