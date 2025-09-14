using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;
using System.Collections.Immutable;

namespace MG.Sonarr.Next.Models.Fields
{
    [SonarrObject]
    public sealed class FieldObject : SonarrObject,
        IComparable<FieldObject>,
        ISerializableNames<FieldObject>
    {
        const int CAPACITY = 8;
        static readonly string _typeName = typeof(FieldObject).GetName();
        protected override bool DisregardMetadataTag => true;
        public bool Advanced => this.GetValue<bool>();
        public string HelpText => this.GetStringOrEmpty();
        public bool IsFloat => this.GetValue<bool>();
        public string Label => this.GetStringOrEmpty();
        public string Name => this.GetStringOrEmpty();
        public int Order => this.GetValue<int>();
        public string Privacy => this.GetStringOrEmpty();
        public IReadOnlyList<SelectOptionObject> SelectOptions { get; private set; }
        public string Type => this.GetStringOrEmpty();

        public FieldObject()
            : base(CAPACITY)
        {
            this.SelectOptions = ImmutableArray<SelectOptionObject>.Empty;
        }

        public int CompareTo(FieldObject? other)
        {
            return Comparer<int?>.Default.Compare(this.Order, other?.Order);
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return existing;
        }

        [SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
        protected override void OnDeserialized(bool alreadyCalled)
        {
            base.OnDeserialized(alreadyCalled);

            this.ReplaceWithReadOnlyStringProperty(nameof(HelpText));
            this.ReplaceWithReadOnlyStringProperty(nameof(Label));
            this.ReplaceWithReadOnlyStringProperty(nameof(Name));
            this.ReplaceWithReadOnlyStringProperty(nameof(Privacy));
            this.ReplaceWithReadOnlyStringProperty(nameof(Type));
            this.ReplaceWithReadOnlyNumberProperty<int>(nameof(Order));
            this.ReplaceWithReadOnlyStructProperty<bool>(nameof(IsFloat));
            this.ReplaceWithReadOnlyStructProperty<bool>(nameof(Advanced));

            if (this.TryGetNonNullProperty(nameof(SelectOptions), out IReadOnlyList<SelectOptionObject>? list))
            {
                this.SelectOptions = list;
            }
            else
            {
                var array = ImmutableArray<SelectOptionObject>.Empty;
                this.Properties.Add(new ReadOnlyCollectionProperty<SelectOptionObject>(nameof(SelectOptions), array));
                this.SelectOptions = array;
            }
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}

