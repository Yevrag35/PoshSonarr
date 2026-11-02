using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients;

[Cmdlet(VerbsCommon.Get, "SonarrDownloadClientSchema")]
public sealed class GetSonarrDownloadClientSchemaCmdlet : SonarrMetadataCmdlet
{
	private WildcardSet _wcNames = null!;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false, Position = 0)]
	[SupportsWildcards]
	public string[] Name { get; set; } = [];

	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.DownloadClientSchema];
	}

	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_wcNames = new();
	}

	protected override void Process(IServiceProvider provider)
	{
		base.Process(provider);
		_wcNames.UnionWith(this.Name);
		var all = this.SendGetRequest<MetadataList<DownloadClientSchemaObject>>(this.Tag.UrlBase);

		if (all.IsError)
		{
			this.WriteConditionalError(all.Error);
			return;
		}

		IEnumerable<DownloadClientSchemaObject> results = _wcNames.Count > 0
			? all.Value.Where(x => _wcNames.IsAnyMatch(x.ImplementationName))
			: all.Value;

		this.WriteCollection(results);
	}
}