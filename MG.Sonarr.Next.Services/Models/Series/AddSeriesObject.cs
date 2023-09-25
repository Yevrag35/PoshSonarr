using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Models.Series;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Models.Series
{
    public sealed class AddSeriesObject : SeriesObject, IJsonOnSerializing
    {
        private string? _path;

        public SeriesAddOptions AddOptions { get; set; } = null!;
        public bool IsFullPath { get; set; }
        public bool IsMonitored { get; set; } = true;
        public int LanguageProfileId { get; set; }
        public int ProfileId { get; set; }
        public int QualityProfileId { get; set; }
        public string Path
        {
            get => _path ??= string.Empty;
            set => _path = value;
        }
        public string SeriesType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool UseSeasonFolders { get; set; }

        public AddSeriesObject()
            : base(50)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.SERIES_ADD];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            this.Properties.Add(new PSAliasProperty(Constants.NAME, Constants.TITLE));
            this.Properties.RemoveMany(Constants.ID, "Added", Constants.QUALITY_PROFILE_ID, Constants.PROFILE_ID, Constants.LANG_PROFILE_ID);

            if (this.TryGetNonNullProperty(Constants.TITLE, out string? title))
            {
                this.Title = title;
            }

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
            this.AddProperties(x => x.AddOptions, x => x.ProfileId, x => x.LanguageProfileId, x => x.QualityProfileId);
            this.UpdateProperty(x => x.Title);
            this.UpdateProperty(x => x.IsMonitored);
            this.UpdateProperty(x => x.SeriesType);
            this.UpdateProperty(x => x.UseSeasonFolders);
            this.SetPath();
        }

        private void SetPath()
        {
            if (this.IsFullPath)
            {
                this.AddProperty(x => x.Path);
            }
            else
            {
                this.Properties.Add(new PSNoteProperty("RootFolderPath", this.Path));
            }
        }
    }
}
