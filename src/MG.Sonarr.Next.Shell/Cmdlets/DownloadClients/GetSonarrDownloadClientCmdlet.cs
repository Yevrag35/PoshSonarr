using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Output;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients;

[Cmdlet(VerbsCommon.Get, "SonarrDownloadClient", DefaultParameterSetName = "ByNameOrId"), OutputType(typeof(IDownloadClientOutput))]
public sealed class GetSonarrDownloadClientCmdlet : SonarrMetadataCmdlet
{
	const int CAPACITY = 2;
	static readonly string _namePropertyName = nameof(Name);

	SortedSet<int> _ids = null!;
	WildcardSet _wcNames = null!;
	protected override int Capacity => CAPACITY;

	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
	public int[] Id { get; set; } = [];

	[Parameter(Position = 0, ParameterSetName = "ByNameOrId")]
	public Either<string, int>[] Name { get; set; } = [];

	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);

		_ids = this.GetPooledObject<SortedSet<int>>();
		_wcNames = this.GetPooledObject<WildcardSet>();

		this.SetReturnables(_ids, _wcNames);
	}
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.DOWNLOAD_CLIENT];
	}

	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
		if (this.HasParameter(this.Name))
		{
			this.Name.SplitToSets(_ids, _wcNames, !this.MyInvocation.IsBoundPositionally(_namePropertyName));
		}
	}
	protected override void Process(IServiceProvider provider)
	{
		IList<DownloadClientObject> dlObjs = _ids.Count > 0 && _wcNames.Count == 0
			? this.GetById<DownloadClientObject>(_ids)
			: this.GetByName(_wcNames, _ids);

		this.WriteCollection(dlObjs);
	}

	private MetadataList<DownloadClientObject> GetByName(WildcardSet names, SortedSet<int> ids)
	{
		var all = this.GetAll<DownloadClientObject>();

		if (all.Count > 0 && (names.Count > 0 || ids.Count > 0))
		{
			FnPtr<DownloadClientObject, WildcardSet, SortedSet<int>, bool> removePtr;
			unsafe
			{ removePtr = new(&removeIf); }

			all.RemoveAll(names, ids, removePtr);
		}

		return all;

		static bool removeIf(DownloadClientObject item, WildcardSet names, SortedSet<int> ids)
		{
			return !ids.Contains(item.Id) && !names.IsAnyMatch(item.Name);
		}
	}
}
