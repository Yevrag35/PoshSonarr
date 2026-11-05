using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Extensions;
using System.Net;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series;

[Cmdlet(VerbsCommon.Search, "SonarrSeries", DefaultParameterSetName = "BySeriesName")]
public sealed class SearchSonarrSeriesCmdlet : SonarrApiCmdletBase
{
	static readonly string SEARCH_STR_QUERY = Constants.Series + "/lookup?term={0}";
	static readonly string SEARCH_ID_QUERY = Constants.Series + "/lookup?term=tvdb:{0}";
	Wildcard _wildcardStr;
	IMetadataResolver _resolver = null!;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesName")]
	public string Name
	{
		get => string.Empty;
		set => _wildcardStr = value;
	}

	[Parameter(Mandatory = true, ParameterSetName = "ByTVDbId")]
	public long TVDbId { get; set; }

	[Parameter(Mandatory = false, ParameterSetName = "BySeriesName")]
	public SwitchParameter Strict { get; set; }

	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_resolver = provider.GetRequiredService<IMetadataResolver>();
	}
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

	private static string GetSearchByNamePath(in Wildcard name)
	{
		string use = name.ContainsWildcards
			? ((string)name).Replace('*', ' ').Replace('?', ' ').Trim()
			: (string)name;

		return string.Format(CultureInfo.InvariantCulture, SEARCH_STR_QUERY, WebUtility.UrlEncode(use));
	}

	private static bool StrictlyMatches(string value, in Wildcard stringToMatch)
	{
		return stringToMatch.IsMatch(value)
			   ||
			   value.Contains((string)stringToMatch, StringComparison.OrdinalIgnoreCase);
	}
}
