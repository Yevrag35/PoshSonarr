using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.WantedMissing;

[Cmdlet(VerbsCommon.Get, "SonarrWantedMissing", DefaultParameterSetName = "ByPage")]
public sealed class GetSonarrWantedMissingCmdlet : SonarrApiCmdletBase
{
	QueryCol _parameters = null!;

	[Parameter(Mandatory = true, ParameterSetName = "AllRecords")]
	public SwitchParameter All { get; set; }

	[Parameter(Mandatory = false, ParameterSetName = "ByPage")]
	[Alias("Page")]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int PageNumber { get; set; }

	[Parameter(Mandatory = false, ParameterSetName = "ByPage")]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int PageSize { get; set; }

	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_parameters = new(2);
	}
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
			return Constants.WantedMissing;
		}

		Span<char> span = stackalloc char[Constants.WantedMissing.Length + 1 + parameters.MaxLength];
		Constants.WantedMissing.CopyTo(span);
		int position = Constants.WantedMissing.Length;

		_ = parameters.TryFormat(span.Slice(position), out int written);
		return new string(span.Slice(0, position + written));
	}

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
