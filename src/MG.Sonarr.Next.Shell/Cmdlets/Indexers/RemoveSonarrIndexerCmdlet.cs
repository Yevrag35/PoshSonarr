using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Indexers;

[Cmdlet(VerbsCommon.Remove, "SonarrIndexer", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
[MetadataCanPipe(Tag = Meta.INDEXER)]
public sealed class RemoveSonarrIndexerCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;

	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public IndexerObject[] InputObject { get; set; } = [];

	[Parameter]
	public SwitchParameter Force { get; set; }
	protected override int Capacity => 1;

	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.INDEXER];
	}
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = this.GetPooledObject<SortedSet<int>>();
		this.SetReturnables(_ids);
	}

	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
	}

	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length > 0)
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

		foreach (int id in _ids)
		{
			this.DeleteIndexer(id);
		}
	}

	private void DeleteIndexer(int id)
	{
		string url = this.Tag.GetUrlForId(id);
		if (!this.Force
			&&
			!this.ShouldProcess(url, "Deleting Indexer"))
		{
			return;
		}

		var response = this.SendDeleteRequest(url);
		if (response.IsError)
		{
			this.WriteError(response.Error);
			return;
		}

		this.WriteVerbose($"Deleted Indexer -> {id}");
	}
}

