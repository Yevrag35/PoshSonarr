using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Models.Media;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.MediaManagement;

[Cmdlet(VerbsData.Update, "SonarrMediaManagement", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
[MetadataCanPipe(Tag = Meta.MediaManagement)]
public sealed class UpdateSonarrMediaManagementCmdlet : SonarrApiCmdletBase
{
	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
	[ValidateNotNull]
	[ValidateId(ValidateRangeKind.Positive)]
	public MediaManagementObject InputObject { get; set; } = null!;

	[Parameter]
	public SwitchParameter Force { get; set; }

	protected override void Process(IServiceProvider provider)
	{
		var tag = provider.GetMetadataTag(Meta.MediaManagement);
		string url = tag.GetUrlForId(this.InputObject.Id);

		if (!this.ShouldProcessWithForce(url, "Update Media Management Settings", this.Force))
		{
			return;
		}

		string rbPath = this.InputObject.RecycleBinPath;
		if (!string.IsNullOrWhiteSpace(rbPath))
		{
			if (!this.TryGetDirectorySeparator(provider, out char separator))
			{
				return;
			}

			if (!this.InputObject.IsRecycleBinPathProper(separator, out rbPath))
			{
				this.InputObject.RecycleBinPath = rbPath + separator;
			}
		}

		var response = this.SendPutRequest(url, this.InputObject);
		if (response.IsError)
		{
			this.InputObject.RecycleBinPath = rbPath;
			this.InputObject.Reset();
			this.WriteError(response.Error);
		}
		else
		{
			this.InputObject.Commit();
		}
	}

	private bool TryGetDirectorySeparator(IServiceProvider provider, out char separator)
	{
		var tag = provider.GetMetadataTag(Meta.Status);
		var result = this.SendGetRequest<SystemStatusObject>(tag.UrlBase);
		if (result.IsError)
		{
			separator = default;
			this.WriteError(result.Error);
			return false;
		}

		separator = result.Value.DirectorySeparator;
		return true;
	}
}
