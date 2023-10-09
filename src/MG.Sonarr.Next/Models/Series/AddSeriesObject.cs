﻿using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Shell.Models.Series;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Series
{
    public sealed class AddSeriesObject : SeriesObject, IComparable<AddSeriesObject>, IJsonOnSerializing
    {
        private string? _path;
        private string? _pathProp;
        const int CAPACITY = 50;

        public SeriesAddOptions? AddOptions { get; set; }
        public bool IsFullPath { get; set; }
        public bool IsMonitored
        {
            get => this.GetValue<bool>();
            set => this.SetValue(value);
        }
        public int ProfileId
        {
            get => this.GetValue<int>();
            set => this.SetValue(value);
        }
        public string Path
        {
            get => _path ??= string.Empty;
            set => _path = value;
        }
        public string SeriesType
        {
            get => this.GetValue<string>() ?? string.Empty;
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

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
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

        const string ROOT_FOLDER_PATH = "RootFolderPath";
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
