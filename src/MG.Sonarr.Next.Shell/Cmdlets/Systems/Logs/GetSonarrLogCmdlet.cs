using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using System.ComponentModel;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Logs;

/// <summary>
/// Represents the <c>Get-SonarrLog</c> cmdlet, which gets one page of the entries in Sonarr's log.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet gets the page from <c>/log</c> and writes each of its entries to the pipeline as a
/// <see cref="LogObject"/>, sorted by time. <c>-PageNumber</c> and <c>-PageSize</c> pick the page, and
/// <c>-SortKey</c> and <c>-SortDirection</c> the order that Sonarr pages the entries in, newest first by default.
/// </para>
/// <para>
/// When the request fails, the cmdlet writes its error and no entries.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrLog")]
public sealed class GetSonarrLogCmdlet : SonarrMetadataCmdlet
{
	private QueryCol _parameters = null!;

	/// <summary>
	/// Gets or sets how many entries the page holds.
	/// </summary>
	/// <remarks>
	/// The size has to be greater than zero. When the parameter isn't given, the request leaves the size to Sonarr.
	/// </remarks>
	/// <value>The number of entries on a page. The default is Sonarr's, 10.</value>
	[Parameter]
	[ValidateRange(ValidateRangeKind.Positive)]
	[PSDefaultValue(Value = 10)]
	public int PageSize { get; set; }

	/// <summary>
	/// Gets or sets the number of the page to get.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Pages are numbered from 1, so the number has to be greater than zero. When the parameter isn't given, the request
	/// leaves the page to Sonarr.
	/// </para>
	/// <para>
	/// The parameter's alias is <c>-Page</c>.
	/// </para>
	/// </remarks>
	/// <value>The page's number. The default is Sonarr's, the first page.</value>
	[Parameter]
	[Alias("Page")]
	[ValidateRange(ValidateRangeKind.Positive)]
	[PSDefaultValue(Value = 1)]
	public int PageNumber { get; set; }

	/// <summary>
	/// Gets or sets the direction that Sonarr sorts the entries in before it pages them.
	/// </summary>
	/// <remarks>
	/// The parameter's alias is <c>-Direction</c>.
	/// </remarks>
	/// <value>
	/// The direction of the sort. The default is <see cref="ListSortDirection.Descending"/>, which puts the newest entries
	/// on the first page when the key is <c>time</c>.
	/// </value>
	[Parameter]
	[Alias("Direction")]
	[ValidateRange((int)ListSortDirection.Ascending, (int)ListSortDirection.Descending)]
	[PSDefaultValue(Value = ListSortDirection.Descending)]
	public ListSortDirection SortDirection { get; set; } = ListSortDirection.Descending;

	/// <summary>
	/// Gets or sets the field of the entries that Sonarr sorts them by before it pages them.
	/// </summary>
	/// <remarks>
	/// The key can't be empty or only white space.
	/// </remarks>
	/// <value>The name of the field to sort by. The default is <c>time</c>.</value>
	[Parameter]
	[ValidateNotNullOrWhiteSpace]
	[PSDefaultValue(Value = "time")]
	public string SortKey { get; set; } = "time";

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the request's query.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_parameters = new(10);
	}
	/// <summary>
	/// Adds the paging and sorting parameters to the request's query.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		this.SetPagingParams();
	}
	/// <summary>
	/// Adds to the request's query the page number and size, when they were given, and the sort key and direction.
	/// </summary>
	private void SetPagingParams()
	{
		if (this.HasParameter(this.PageNumber))
		{
			_parameters.Add(PagingConstants.PageNumber, this.PageNumber);
		}

		if (this.HasParameter(this.PageSize))
		{
			_parameters.Add(PagingConstants.PageSize, this.PageSize);
		}

		_parameters.Add(PagingConstants.SortKey, this.SortKey);
		_parameters.Add(PagingConstants.SortDirection, this.SortDirection, this.SortDirection.Length);
	}

	/// <summary>
	/// Gets the page of log entries and writes its entries to the pipeline.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		string url = this.Tag.GetUrl(_parameters);
		if (this.GetLogs(url, out var result))
		{
			this.WriteCollection(result.Records);
		}
	}

	/// <summary>
	/// Gets the page of log entries at the specified path, and writes the request's error when it fails.
	/// </summary>
	/// <param name="url">The log endpoint's path with the request's query.</param>
	/// <param name="result">
	/// When the method returns, the page that Sonarr returned, or <see langword="null"/> when the request failed.
	/// </param>
	/// <returns><see langword="true"/> if the request succeeded; otherwise, <see langword="false"/>.</returns>
	private bool GetLogs(string url, [NotNullWhen(true)] out RecordResult<LogObject>? result)
	{
		var response = this.SendGetRequest<RecordResult<LogObject>>(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			result = null;
			return false;
		}

		result = response.Value;
		return true;
	}

	/// <summary>
	/// Gets the metadata tag of the log endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The log entries' metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.LogItem];
	}
}
