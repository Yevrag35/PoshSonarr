using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RootFolders;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.RootFolders;

[Cmdlet(VerbsCommon.Remove, "SonarrRootFolder", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
[MetadataCanPipe(Tag = Meta.RootFolder)]
public sealed class RemoveSonarrRootFolderCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;

	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public RootFolderObject[] InputObject { get; set; } = [];

	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	[Parameter]
	public SwitchParameter Force { get; set; }

	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.RootFolder];
	}
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
	}

	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		if (!this.HasParameter(InputObject))
		{
			return;
		}

		foreach (RootFolderObject rootFol in this.InputObject)
		{
			if (rootFol.Id <= 0)
			{
				continue;
			}

			_ = _ids.Add(rootFol.Id);
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
			this.PerformDelete(id, force);
		}
	}

	private void PerformDelete(int id, bool force)
	{
		string url = this.Tag.GetUrlForId(id);

		if (!this.ShouldProcessWithForce(url, "Deleting Root Folder", force))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
		}
	}
}

