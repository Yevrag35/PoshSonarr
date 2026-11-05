using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Releases;

[Cmdlet(VerbsCommon.Remove, "SonarrReleaseProfile", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
[MetadataCanPipe(Tag = Meta.ReleaseProfile)]
public sealed class RemoveSonarrReleaseProfile : SonarrApiCmdletBase
{
	MetadataTag _tag = null!;

	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int Id { get; set; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetPipeline, ValueFromPipeline = true)]
	[ValidateId(ValidateRangeKind.Positive)]
	public ReleaseProfileObject InputObject
	{
		get => null!;
		set => this.Id = value?.Id ?? -1;
	}

	[Parameter]
	public SwitchParameter Force { get; set; }

	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_tag = provider.GetMetadataTag(Meta.ReleaseProfile);
	}

	protected override void Process(IServiceProvider provider)
	{
		string url = _tag.GetUrlForId(this.Id);

		if (!this.ShouldProcessWithForce(url, "Deleting Release Profile", this.Force))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteVerbose($"Deleted Release Profile -> {this.Id}");
	}
}

