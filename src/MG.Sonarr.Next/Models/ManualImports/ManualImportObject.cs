using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.ManualImports
{
    [SonarrObject]
    public sealed class ManualImportObject : IdSonarrObject<ManualImportObject>,
        ISerializableNames<ManualImportObject>
    {
        const int CAPACITY = 13;
        const string EPISODES = "Episodes";
        const string QUALITY = "Quality";
        const string SEASON_NUMBER = "SeasonNumber";
        const string RELEASE_GROUP = "ReleaseGroup";
        const string SERIES = "Series";

        static readonly string _typeName = typeof(ManualImportObject).GetTypeName();

        public string Name => this.GetStringOrEmpty();

        public ManualImportObject()
            : base(CAPACITY)
        {
        }

        public override int CompareTo(ManualImportObject? other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(this.Name, other?.Name);
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.MANUAL_IMPORT];
        }
        public override void OnDeserialized()
        {
            base.OnDeserialized();
            this.StoreAndReplaceName();
            this.AddMissingProperties();
        }

        private void StoreAndReplaceName()
        {
            string name = nameof(this.Name);
            PSPropertyInfo? nameProp = this.Properties[name];
            this.Properties.Remove(name);
            this.Properties.Add(new ReadOnlyStringProperty(name, nameProp?.Value as string));
        }

        private void AddMissingProperties()
        {
            CreateIfMissing(this.Properties, EPISODES, (name) =>
            {
                return new ReadOnlyCollectionProperty<EpisodeObject, SortedSet<EpisodeObject>>(
                    EPISODES, new SortedSet<EpisodeObject>());
            });
            CreateIfMissing(this.Properties, QUALITY, (name) =>
            {
                return new WritableSonarrProperty<QualityRevisionObject>(QUALITY);
            });
            CreateIfMissing(this.Properties, RELEASE_GROUP, (name) => new StringNoteProperty(name, string.Empty));
            CreateIfMissing(this.Properties, SERIES, (name) => new WritableSonarrProperty<SeriesObject>(name));
            CreateIfMissing(this.Properties, SEASON_NUMBER, (name) =>
            {
                return new PSScriptProperty(name,
                    ScriptBlock.Create("if (0 -lt $this.Episodes.Count) { $this.Episodes[0].SeasonNumber }"));
            });
        }

        private static void CreateIfMissing<T>(PSMemberInfoCollection<T> collection, string propertyName, Func<string, T> createOnMissing) where T : PSMemberInfo
        {
            T? prop = collection[propertyName];
            if (prop is null)
            {
                T create = createOnMissing(propertyName);
                collection.Add(create);
            }
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}

