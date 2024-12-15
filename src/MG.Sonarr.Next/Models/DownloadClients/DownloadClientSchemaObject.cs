using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.DownloadClients;

[SonarrObject]
public sealed class DownloadClientSchemaObject : TagUpdateObject<DownloadClientSchemaObject>,
    ISerializableNames<DownloadClientSchemaObject>
{
    const int CAPACITY = 15;
    static readonly string _typeName = typeof(DownloadClientSchemaObject).GetName();

    public string ImplementationName
    {
        get => this.GetStringOrEmpty();
    }

    public DownloadClientSchemaObject() : base(CAPACITY)
    {
    }

    protected override void SetPSTypeName()
    {
        base.SetPSTypeName();
        this.TypeNames.Insert(0, _typeName);
    }

    protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
    {
        return resolver[Meta.DOWNLOAD_CLIENT_SCHEMA];
    }
}