using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.WantedMissing;

[Cmdlet(VerbsCommon.Get, "SonarrWantedMissing", DefaultParameterSetName = "ByPage")]
public sealed class GetSonarrWantedMissingCmdlet : SonarrApiCmdletBase
{
	QueryCol QueryCol { get; set; } = null!;

	[Parameter(Mandatory = true, ParameterSetName = "AllRecords")]
	public SwitchParameter All { get; set; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false, ParameterSetName = "ByPage")]
	[Alias("Page")]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int PageNumber { get; set; }
	//{
	//    get => 0;
	//    set
	//    {
	//        this.QueryCol ??= new(3);
	//        this.QueryCol.Add("page", value);
	//    }
	//}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false, ParameterSetName = "ByPage")]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int PageSize
	{
		get => 0;
		set
		{
			this.QueryCol ??= new(3);
			this.QueryCol.Add("pageSize", value);
		}
	}

	protected override void Process(IServiceProvider provider)
	{
		this.QueryCol ??= [];
		if (this.All)
		{
			IEnumerable<EpisodeObject> records = this.SendAllRecords();
			this.WriteCollection(records);
			return;
		}

		string url = GetUrl(this.QueryCol);
		if (this.GetWantedMissing(url, out RecordResult<EpisodeObject>? result))
		{
			this.WriteCollection(result.Records);
		}
	}

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

	private static string GetUrl(QueryCol parameters)
	{
		if (parameters.Count == 0)
		{
			return Constants.WANTEDMISSING;
		}

		Span<char> span = stackalloc char[Constants.WANTEDMISSING.Length + 1 + parameters.MaxLength];
		Constants.WANTEDMISSING.CopyTo(span);
		int position = Constants.WANTEDMISSING.Length;

		_ = parameters.TryFormat(span.Slice(position), out int written);
		return new string(span.Slice(0, position + written));
	}

	private MetadataList<EpisodeObject> SendAllRecords()
	{
		this.PageNumber = 1;
		this.PageSize = 1;

		string url = GetUrl(this.QueryCol);
		if (!this.GetWantedMissing(url, out RecordResult<EpisodeObject>? result))
		{
			return [];
		}

		this.QueryCol.AddOrUpdate(PagingConstants.PageSize, result.TotalRecords);

		url = GetUrl(this.QueryCol);
		return this.GetWantedMissing(url, out result)
			? result.Records
			: [];
	}
}
