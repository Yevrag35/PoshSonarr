using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags;

[Cmdlet(VerbsCommon.Add, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
	DefaultParameterSetName = "None")]
[MetadataCanPipe(Tag = Meta.DELAY_PROFILE)]
[MetadataCanPipe(Tag = Meta.DOWNLOAD_CLIENT)]
[MetadataCanPipe(Tag = Meta.INDEXER)]
[MetadataCanPipe(Tag = Meta.RELEASE_PROFILE)]
[MetadataCanPipe(Tag = Meta.SERIES)]
[MetadataCanPipe(Tag = Meta.SERIES_ADD)]
public sealed class AddSonarrTagCmdlet : SonarrMetadataCmdlet
{
	static readonly string _namePropertyName = nameof(Name);
	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;
	Dictionary<string, ITagPipeable> _updates = null!;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
	[ValidateIds(ValidateRangeKind.Positive, typeof(ITagPipeable))]
	public ITagPipeable[] InputObject { get; set; } = [];

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
	public int[] Id { get; set; } = [];

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Position = 0)]
	public Either<string, int>[] Name { get; set; } = [];

	protected override int Capacity => 3;
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = this.GetPooledObject<SortedSet<int>>();
		_wcNames = this.GetPooledObject<WildcardSet>();
		_updates = this.GetPooledObject<Dictionary<string, ITagPipeable>>();

		this.SetReturnables(_ids, _wcNames, _updates);
	}

	private void AddUrlsFromMetadata(ITagPipeable[] pipeables)
	{
		foreach (ITagPipeable item in pipeables)
		{
			bool added = _updates.TryAdd(item.MetadataTag.GetUrlForId(item.Id), item);
		}
	}

	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.TAG];
	}

	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Begin(IServiceProvider provider)
	{
		if (this.Id.Length > 0)
		{
			_ids.UnionWith(this.Id);
		}
		else if (this.Name.Length > 0)
		{
			this.Name.SplitToSets(_ids, _wcNames, !this.MyInvocation.IsBoundPositionally(_namePropertyName));
		}

		if (_wcNames.Count > 0)
		{
			var all = this.GetAll<TagObject>();

			foreach (TagObject tag in all)
			{
				if (_wcNames.IsAnyMatch(tag.Label))
				{
					_ = _ids.Add(tag.Id);
				}
			}
		}
	}
	protected override void Process(IServiceProvider provider)
	{
		if (this.InputObject.Length > 0)
		{
			this.AddUrlsFromMetadata(this.InputObject);
		}
	}
	protected override void End(IServiceProvider provider)
	{
		if (this.InvokeCommand.HasErrors)
		{
			return;
		}

		foreach (KeyValuePair<string, ITagPipeable> kvp in _updates)
		{
			if (kvp.Value.Tags.IsSupersetOf(_ids))
			{
				this.WriteVerbose("No tags are being added that didn't already exist on the object.");
				continue;
			}

			if (this.ShouldProcess(
				target: kvp.Key,
				action: string.Format(
					"Adding tags: ({0})",
					string.Join(", ", _ids.Where(x => !kvp.Value.Tags.Contains(x))))))
			{
				if (!this.PerformTagUpdate(kvp))
				{
					continue;
				}
			}
		}
	}

	private bool PerformTagUpdate(KeyValuePair<string, ITagPipeable> kvp)
	{
		kvp.Value.Tags.UnionWith(_ids);

		if (!kvp.Value.MustUpdateViaApi)
		{
			return true;
		}

		var response = this.SendPutRequest(path: kvp.Key, body: kvp.Value);
		if (response.IsError)
		{
			kvp.Value.Reset();
			this.WriteConditionalError(response.Error);
			return false;
		}
		else
		{
			kvp.Value.CommitTags();
			return true;
		}
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
