using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.DownloadClients;

[SonarrObject]
public sealed class DownloadClientSchemaObject : TagUpdateObject<DownloadClientSchemaObject>,
    ISerializableNames<DownloadClientSchemaObject>
{
    const int CAPACITY = 15;
    static readonly string _typeName = typeof(DownloadClientSchemaObject).GetName();

    public string ConfigContract
    {
        get => this.GetStringOrEmpty();
    }
    public bool Enable
    {
        get => this.GetValue<bool>();
        set => this.SetValue(value);
    }
    public string Implementation
    {
        get => this.GetStringOrEmpty();
    }
    public string ImplementationName
    {
        get => this.GetStringOrEmpty();
    }
    public string InfoLink
    {
        get => this.GetStringOrEmpty();
    }
    public string Name
    {
        get => this.GetStringOrEmpty();
        set => this.SetValue(value);
    }
    public string Protocol
    {
        get => this.GetStringOrEmpty();
    }
    public int Priority
    {
        get => this.GetValue<int>();
        set => this.SetValue(value);
    }

    public DownloadClientSchemaObject() : base(CAPACITY)
    {
    }

    public override PSObject Copy()
    {
        DownloadClientSchemaObject copy = new();
        foreach (PSPropertyInfo prop in this.Properties)
        {
            copy.Members.Add(prop.Copy());
        }

        copy.SetPSTypeName();
        return copy;
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

    [SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
    protected override void OnDeserialized(bool alreadyCalled)
    {
        base.OnDeserialized(alreadyCalled);

        this.ReplaceWithReadOnlyStringProperty(nameof(ConfigContract));
        this.ReplaceWithReadOnlyStringProperty(nameof(Implementation));
        this.ReplaceWithReadOnlyStringProperty(nameof(ImplementationName));
        this.ReplaceWithReadOnlyStringProperty(nameof(InfoLink));
        this.ReplaceWithReadOnlyStringProperty(nameof(Protocol));
    }
}