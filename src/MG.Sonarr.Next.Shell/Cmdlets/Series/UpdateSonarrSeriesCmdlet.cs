using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series;

/// <summary>
/// Represents the <c>Update-SonarrSeries</c> cmdlet, which saves the changes made to series objects back to Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes series that <c>Get-SonarrSeries</c> wrote, after their properties were changed in PowerShell,
/// from the pipeline or through <c>-InputObject</c>, and collects them until its input ends. Then it sends each
/// confirmed series to Sonarr with a PUT request, and writes nothing to the pipeline. After a request that succeeds,
/// the series is committed, and after one that fails, the cmdlet writes the request's error, resets the series, and
/// goes on to the next. When no series reaches it, it writes a warning.
/// </para>
/// <para>
/// The cmdlet confirms each series before it sends it. Its confirm impact is low, so it prompts only when
/// <c>-Confirm</c> or <c>$ConfirmPreference</c> asks for it, and <c>-WhatIf</c> shows what it would update and sends
/// nothing.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Set-SonarrSeries</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Update, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
[Alias("Set-SonarrSeries")]
[MetadataCanPipe(Tag = Meta.Series)]
public sealed class UpdateSonarrSeriesCmdlet : SonarrApiCmdletBase
{
	List<SeriesObject> _list = null!;

	/// <summary>
	/// Gets a value that indicates whether the HTTP pipeline writes each request's JSON body to the debug stream before
	/// it sends the request.
	/// </summary>
	/// <remarks>
	/// The cmdlet writes each series' body to the debug stream itself, once the series is confirmed, so the HTTP
	/// pipeline doesn't write it again.
	/// </remarks>
	/// <value>Always <see langword="false"/>.</value>
	public override bool CanDebugSerializeBefore => false;

	/// <summary>
	/// Gets or sets the series whose changes the cmdlet saves.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and each piped series arrives in an array of its own. Each series' ID has
	/// to be greater than zero.
	/// </remarks>
	/// <value>The changed series. The default is an empty array.</value>
	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public SeriesObject[] InputObject { get; set; } = [];

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the list of series that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_list = new(1);
	}
	/// <summary>
	/// Adds each series from <c>-InputObject</c> to the series that the cmdlet sends, and writes a warning for one whose
	/// ID isn't greater than zero, which it leaves out.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		foreach (SeriesObject series in this.InputObject)
		{
			if (series.Id <= 0)
			{
				this.WriteWarning("A series with an invalid ID was passed. It will be ignored.");
				continue;
			}

			_list.Add(series);
		}
	}
	/// <summary>
	/// Sends each confirmed series to Sonarr with a PUT request, and commits the series after a request that succeeds
	/// or writes the error and resets it after one that fails.
	/// </summary>
	/// <remarks>
	/// Each series' JSON body goes to the debug stream once the series is confirmed, as the request sends it. When no
	/// series reached the cmdlet, it writes a warning and sends nothing.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		MetadataTag tag = provider.GetMetadataTag(Meta.Series);

		if (_list is null || _list.Count <= 0)
		{
			this.WriteWarning("No series were passed via the pipeline. Make sure to pass the correct object type.");
			return;
		}

		foreach (SeriesObject item in _list)
		{
			string path = tag.GetUrlForId(item.Id);
			if (this.ShouldProcess(path, "Update Series"))
			{
				this.SerializeIfDebug(
					value: item,
					options: provider.GetService<ISonarrJsonOptions>()?.ForSerializing);

				var response = this.SendPutRequest(path, item);
				if (response.IsError)
				{
					this.WriteError(response.Error);
					item.Reset();
				}
				else
				{
					item.Commit();
				}
			}
		}
	}
}
