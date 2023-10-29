using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Models.Qualities
{
    [SonarrObject]
    public sealed class QualityObject : SonarrObject,
        IComparable<QualityObject>,
        ISerializableNames<QualityObject>
    {
        const int CAPACITY = 4;
        static readonly string _typeName = typeof(QualityObject).GetTypeName();
        static MetadataTag? _qualityTag;
        readonly bool _wasCtored;

        public int Id { get; private set; }
        public string Name => this.GetStringOrEmpty();
        public int Resolution => this.GetValue<int>();
        public string Source => this.GetStringOrEmpty();

        public QualityObject()
            : base(CAPACITY)
        {
        }
        public QualityObject(QualityRevisionObject qualityAndRevision)
            : this(qualityAndRevision.Quality)
        {
        }
        private QualityObject(QualityObject copyFrom)
            : this(copyFrom.Id, copyFrom.Name, copyFrom.Resolution, copyFrom.Source)
        {
        }
        private QualityObject(int id, string name, int resolution, string source)
            : this()
        {
            _wasCtored = true;

            ValidateNumber(id);
            ValidateNumber(resolution);
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentException.ThrowIfNullOrEmpty(source);

            this.Id = id;
            this.Properties.Add(new ReadOnlyNumberProperty<int>(nameof(this.Id), id));
            this.Properties.Add(new ReadOnlyStringProperty(nameof(this.Name), name));
            this.Properties.Add(new ReadOnlyNumberProperty<int>(nameof(this.Resolution), resolution));
            this.Properties.Add(new ReadOnlyStringProperty(nameof(this.Source), source));
        }

        public int CompareTo(QualityObject? other)
        {
            return Comparer<int?>.Default.Compare(this.Id, other?.Id);
        }
        //public override PSObject Copy()
        //{
        //    //if (this.MetadataProperty.Tag.Value != Meta.QUALITY && _qualityTag is not null)
        //    //{
        //    //    this.MetadataProperty.Tag = _qualityTag;
        //    //}

        //    var qo = new QualityObject(this);
        //    qo.OnDeserialized();
        //    return qo;
        //}
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return _qualityTag ??= resolver[Meta.QUALITY];
        }
        public override void OnDeserialized()
        {
            //if (_qualityTag is not null)
            //{
            //    this.MetadataProperty.Tag = _qualityTag;
            //}

            if (_wasCtored)
            {
                return;
            }

            base.OnDeserialized();

            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }

            ReplaceWithReadOnly(this.Properties, nameof(this.Name));
            ReplaceWithReadOnly(this.Properties, nameof(this.Resolution));
            ReplaceWithReadOnly(this.Properties, nameof(this.Source));
        }
        private static void ReplaceWithReadOnly(PSMemberInfoCollection<PSPropertyInfo> collection, string propertyName)
        {
            PSPropertyInfo? prop = collection[propertyName];
            if (prop is WritableProperty writable)
            {
                collection.Remove(propertyName);
                collection.Add(writable.ConvertToReadOnly());
            }
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
        private static void ValidateNumber(int number, [CallerArgumentExpression(nameof(number))] string argumentName = "")
        {
            if (number < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, number, "The number must be non-negative.");
            }
        }

        public static readonly QualityObject Default = new(0, "Unknown", 0, "unknown");
    }
}

