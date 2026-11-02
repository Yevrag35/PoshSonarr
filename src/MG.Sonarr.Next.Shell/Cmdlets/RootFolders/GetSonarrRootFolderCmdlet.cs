using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.RootFolders;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.RootFolders;

[Cmdlet(VerbsCommon.Get, "SonarrRootFolder")]
public sealed class GetSonarrRootFolderCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false, Position = 0)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
	}

	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.RootFolder];
	}

	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}

	protected override void Process(IServiceProvider provider)
	{
		IEnumerable<RootFolderObject> folders = this.HasParameter(this.Id)
			? this.GetById<RootFolderObject>(_ids)
			: this.GetAll<RootFolderObject>();

		this.WriteCollection(folders);
	}
}
