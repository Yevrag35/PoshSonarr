using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Shell.Models.Series;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Series
{
    [SonarrObject]
    public sealed class AddSeriesObject : SeriesObject,
        IComparable<AddSeriesObject>,
        IJsonOnSerializing,
        ISerializableNames<AddSeriesObject>
    {
        const int CAPACITY = 50;
        const string ROOT_FOLDER_PATH = "RootFolderPath";
        static readonly string _typeName = typeof(AddSeriesObject).GetTypeName();
        private string? _path;
        private string? _pathProp;

        public SeriesAddOptions? AddOptions { get; set; }
        public override int Id => -1;
        public bool IsFullPath { get; set; }
        public bool IsMonitored
        {
            get => this.GetValue<bool>();
            set => this.SetValue(value);
        }
        public int ProfileId
        {
            set => this.SetValue(value);
        }
        public string Path
        {
            get => _path ??= string.Empty;
            set => _path = value;
        }
        public string SeriesType
        {
            get => this.GetStringOrEmpty();
            set => this.SetValue(value);
        }
        public bool UseSeasonFolders
        {
            get => this.GetValue<bool>();
            set => this.SetValue(value);
        }

        public AddSeriesObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(AddSeriesObject? other)
        {
            return this.CompareTo((SeriesObject?)other);
        }
        public override int CompareTo(SeriesObject? other)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(this.Title, other?.Title);
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.SERIES_ADD];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            this.Properties.Add(new PSAliasProperty(Constants.NAME, Constants.TITLE));
            this.Properties.RemoveMany(Constants.ID, "Added");

            if (this.TryGetNonNullProperty(Constants.SERIES_TYPE, out string? seriesType))
            {
                this.SeriesType = seriesType;
            }

            if (this.TryGetNonNullProperty(Constants.SEASONS, out object[]? array) && array.Length > 1)
            {
                this.Properties[Constants.USE_SEASON_FOLDER].Value = true;
                this.UseSeasonFolders = true;
            }
        }
        public override void OnSerializing()
        {
            this.UpdateProperty(x => x.AddOptions);
            this.UpdateProperty(x => x.Title);
            this.SetPath();
            base.OnSerializing();
        }
        private void SetPath()
        {
            if (this.IsFullPath)
            {
                this.UpdateProperty(x => x.Path);
                _pathProp = nameof(this.Path);
            }
            else
            {
                this.SetValue(this.Path, ROOT_FOLDER_PATH);
                _pathProp = ROOT_FOLDER_PATH;
            }
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            Debugger.Assert(() =>
            {
                return this.TypeNames.Count > 0 && this.TypeNames[0] == typeof(SeriesObject).GetTypeName();
            });

            this.TypeNames[0] = _typeName;  // Should overwrite 'SeriesObject'.
        }
        internal override bool ShouldBeReadOnly(string propertyName, Type parentType)
        {
            return true;
        }
        public override void Reset()
        {
            if (!string.IsNullOrEmpty(_pathProp))
            {
                this.Properties.Remove(_pathProp);
                this.IsFullPath = false;
            }

            this.AddOptions = null;
            base.Reset();
        }
    }
}
