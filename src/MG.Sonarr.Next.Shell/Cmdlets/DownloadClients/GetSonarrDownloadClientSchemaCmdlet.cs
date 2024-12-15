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

    protected override int Capacity => 1;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [Parameter(Mandatory = false, Position = 0)]
    [SupportsWildcards]
    public string[] Name { get; set; } = [];

    protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
    {
        return resolver[Meta.DOWNLOAD_CLIENT_SCHEMA];
    }

    protected override void OnCreatingScope(IServiceProvider provider)
    {
        base.OnCreatingScope(provider);
        _wcNames = this.GetPooledObject<WildcardSet>();
        this.SetReturnables(_wcNames);
    }

    protected override void Process(IServiceProvider provider)
    {
        base.Process(provider);
        _wcNames.UnionWith(this.Name.Where(x => !string.IsNullOrWhiteSpace(x)));
        var all = this.SendGetRequest<MetadataList<DownloadClientSchemaObject>>(this.Tag.UrlBase);

        if (all.IsError)
        {
            this.WriteConditionalError(all.Error);
            return;
        }

        IEnumerable<DownloadClientSchemaObject> results = _wcNames.Count > 0
            ? all.Data.Where(x => _wcNames.IsAnyMatch(x.ImplementationName))
            : all.Data;

        this.WriteCollection(results);
    }
}