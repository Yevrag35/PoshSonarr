using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags;

[Cmdlet(VerbsCommon.Get, "SonarrTag", DefaultParameterSetName = PSConstants.PSetByName)]
[MetadataCanPipe(Tag = Meta.DelayProfile)]
[MetadataCanPipe(Tag = Meta.DownloadClient)]
[MetadataCanPipe(Tag = Meta.Indexer)]
[MetadataCanPipe(Tag = Meta.ReleaseProfile)]
[MetadataCanPipe(Tag = Meta.Series)]
[MetadataCanPipe(Tag = Meta.SeriesAdd)]
public sealed class GetSonarrTagCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;

	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateIds(ValidateRangeKind.Positive, typeof(ITagPipeable))]
	public ITagPipeable[] InputObject { get; set; } = [];

	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	[Parameter(Mandatory = false, Position = 0, ParameterSetName = PSConstants.PSetByName)]
	[SupportsWildcards]
	[ValidateIds(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
	public Either<string, int>[] Name { get; set; } = [];

	protected override int Capacity => 2;
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = this.GetPooledObject<SortedSet<int>>();
		_wcNames = this.GetPooledObject<WildcardSet>();

		this.SetReturnables(_ids, _wcNames);
	}
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.Tag];
	}

	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "<Pending>")]
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
		if (this.HasParameter(this.Name))
		{
			this.Name.SplitToSets(_ids, _wcNames, !this.MyInvocation.IsBoundPositionally(nameof(Name)));
		}
	}
	protected override void Process(IServiceProvider provider)
	{
		if (this.HasParameter(this.InputObject))
		{
			_ids.UnionWith(this.InputObject.SelectMany(x => x.Tags)
										   .Where(x => x > 0));
		}
	}
	protected override void End(IServiceProvider provider)
	{
		if (this.InvokeCommand.HasErrors)
		{
			return;
		}

		IList<TagObject> tags = _ids.Count > 0 && _wcNames.Count == 0
			? this.GetById<TagObject>(_ids)
			: this.ParameterSetName == PSConstants.PSetPipeline || this.HasParameter(this.Name)
				? this.GetAllAndFilter<TagObject>(_ids, _wcNames)
				: this.GetAll<TagObject>();

		this.WriteCollection(tags);
	}

	bool _disposed;
	protected override void Dispose(bool disposing, IServiceScopeFactory? factory)
	{
		if (disposing && !_disposed)
		{
			_ids = null!;
			_wcNames = null!;
			_disposed = true;
		}

		base.Dispose(disposing, factory);
	}
}
