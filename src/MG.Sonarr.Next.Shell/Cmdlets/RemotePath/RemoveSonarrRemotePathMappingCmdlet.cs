using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RemotePaths;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.RemotePath;

[Cmdlet(VerbsCommon.Remove, "SonarrRemotePathMapping", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High, DefaultParameterSetName = PSConstants.PSetExplicitId)]
[MetadataCanPipe(Tag = Meta.RemotePathMapping)]
public sealed class RemoveSonarrRemotePathMappingCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;

	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = Array.Empty<int>();

	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public RemotePathObject[] InputObject { get; set; } = Array.Empty<RemotePathObject>();

	[Parameter]
	public SwitchParameter Force { get; set; }

	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.RemotePathMapping];
	}
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		_ids = [];
	}

	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	protected override void Process(IServiceProvider provider)
	{
		if (PSConstants.PSetPipeline == this.ParameterSetName)
		{
			_ids.UnionWith(this.InputObject.Select(x => x.Id));
		}
	}
	protected override void End(IServiceProvider provider)
	{
		if (_ids.Count <= 0)
		{
			return;
		}

		bool force = this.Force.ToBool();
		foreach (int id in _ids)
		{
			this.DeleteRemotePathMapping(in id, this.Tag, in force);
		}
	}

	private void DeleteRemotePathMapping(in int id, MetadataTag tag, in bool force)
	{
		string url = tag.GetUrlForId(id);
		if (!force
			&&
			!this.ShouldProcess(url, "Deleting Remote Path Mapping"))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteVerbose($"Deleted Remote Path Mapping -> {id}");
	}
}

