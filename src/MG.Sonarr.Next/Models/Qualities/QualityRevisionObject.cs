using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.ManualImports;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.Qualities
{
    [SonarrObject]
    public sealed class QualityRevisionObject : SonarrObject,
        IComparable<QualityRevisionObject>,
        ISerializableNames<QualityRevisionObject>
    {
        const int CAPACITY = 2;
        static readonly string _typeName = typeof(QualityRevisionObject).GetTypeName();

        protected override bool DisregardMetadataTag => true;
        public QualityObject Quality
        {
            get => this.GetValue<QualityObject>() ?? QualityObject.Default;
            private set => this.UpdateProperty(nameof(this.Quality), value);
        }
        public RevisionObject Revision
        {
            get => this.GetValue<RevisionObject>() ?? RevisionObject.Default;
            private set => this.UpdateProperty(nameof(this.Revision), value);
        }

        public QualityRevisionObject()
            : base(CAPACITY)
        {
        }
        public QualityRevisionObject(QualityObject quality)
            : this(quality, null)
        {
        }
        public QualityRevisionObject(QualityObject quality, bool isProper, bool isReal)
            : this(quality, RevisionObject.Create(isProper, isReal))
        {
        }
        private QualityRevisionObject(QualityObject quality, RevisionObject? revision)
            : this()
        {
            ArgumentNullException.ThrowIfNull(quality);
            this.Quality = quality;
            this.Revision = revision ?? RevisionObject.Default;
        }

        public int CompareTo(QualityRevisionObject? other)
        {
            return this.Quality.CompareTo(other?.Quality);
        }
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return existing;
        }
        internal override bool ShouldBeReadOnly(string propertyName, Type parentType)
        {
            if (nameof(this.Quality) == propertyName && typeof(ManualImportObject).Equals(parentType))
            {
                return false;
            }

            return base.ShouldBeReadOnly(propertyName, parentType);
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}

