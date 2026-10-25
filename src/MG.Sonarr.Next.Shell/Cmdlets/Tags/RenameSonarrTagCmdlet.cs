using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags;

[Cmdlet(VerbsCommon.Rename, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
[MetadataCanPipe(Tag = Meta.TAG)]
public sealed class RenameSonarrTagCmdlet : SonarrApiCmdletBase
{
	TagObject? _pipedObject;
	MetadataTag _tag = null!;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int Id { get; set; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
	[ValidateId(ValidateRangeKind.Positive)]
	public TagObject? InputObject
	{
		get => _pipedObject;
		set
		{
			if (value is not null)
			{
				_pipedObject = value;
				this.Id = value.Id;
			}
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_PIPELINE, ValueFromPipelineByPropertyName = true)]
	[Parameter(Mandatory = true, Position = 1, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
	[ValidateNotNullOrEmpty]
	public string NewName { get; set; } = string.Empty;

	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_tag = provider.GetRequiredService<IMetadataResolver>()[Meta.TAG];
	}

	protected override void Process(IServiceProvider provider)
	{
		TagRename rename = TagRename.Create(this.Id, this.NewName);

		string url = _tag.GetUrlForId(rename.Id);

		if (this.ShouldProcess(url, $"Renaming Tag -> {rename.Label}"))
		{
			SonarrClientResult response = this.SendPutRequest(url, rename);

			if (response.IsError)
			{
				this.WriteError(response.Error);
				return;
			}

			this.WriteVerbose($"Renamed Tag -> {this.Id}");
		}
	}
}
