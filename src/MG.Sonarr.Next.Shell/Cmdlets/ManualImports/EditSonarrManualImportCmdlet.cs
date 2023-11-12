using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.ManualImports
{
    [Cmdlet(VerbsData.Edit, "SonarrManualImport")]
    [MetadataCanPipe(Tag = Meta.MANUAL_IMPORT)]
    public sealed class EditSonarrManualImportCmdlet : SonarrApiCmdletBase
    {
        OneOf<int, SeriesObject> _series;
        OneOf<int, EpisodeObject> _episode;
        OneOf<int, QualityRevisionObject> _quality;

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [Alias("Record")]
        [ValidateNotNull]
        [ValidateId(ValidateRangeKind.Positive)]
        public ManualImportObject InputObject { get; set; } = null!;

        [Parameter]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [ValidateId(ValidateRangeKind.Positive, InputNullBehavior.PassAsZero)]
        [ValidateType(typeof(int), typeof(SeriesObject))]
        public object Series
        {
            get => null!;
            set => _series = value is int seriesId
                ? seriesId
                : (SeriesObject)value;
        }

        [Parameter]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [ValidateId(ValidateRangeKind.Positive, InputNullBehavior.PassAsZero)]
        [ValidateType(typeof(int), typeof(EpisodeObject))]
        public object Episode
        {
            get => null!;
            set => _episode = value is int episodeId
                ? episodeId
                : (EpisodeObject)value;
        }

        [Parameter]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [ValidateId(ValidateRangeKind.Positive, InputNullBehavior.PassAsZero)]
        [ValidateType(typeof(int), typeof(QualityRevisionObject))]
        public object Quality
        {
            get => null!;
            set => _quality = value is int qualityId
                ? qualityId
                : (QualityRevisionObject)value;
        }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void Begin(IServiceProvider provider)
        {
            var edit = provider.GetRequiredService<ManualImportEdit>();
            if (this.HasParameter(x => x.Episode))
            {
                edit.Episode = this.GetObject(_episode, provider.GetMetadataTag(Meta.EPISODE));
            }

            if (this.HasParameter(x => x.Quality))
            {
                edit.Quality = this.GetQualityRevision(_quality, provider.GetMetadataTag(Meta.QUALITY));
            }

            if (this.HasParameter(x => x.Series))
            {
                edit.Series = this.GetObject(_series, provider.GetMetadataTag(Meta.SERIES));
            }
        }

        protected override void Process(IServiceProvider provider)
        {
            var edit = provider.GetRequiredService<ManualImportEdit>();
            edit.EditImport(this.InputObject);

            if (this.PassThru)
            {
                this.WriteObject(this.InputObject);
            }
        }

        private T? GetObject<T>(OneOf<int, T> oneOf, MetadataTag tag)
        {
            if (oneOf.TryPickT1(out T value, out int id))
            {
                return value;
            }

            string url = tag.GetUrlForId(id);
            var response = this.SendGetRequest<T>(url);
            if (response.IsError)
            {
                this.WriteConditionalError(response.Error);
                return default;
            }
            else
            {
                return response.Data;
            }
        }

        private QualityRevisionObject? GetQualityRevision(OneOf<int, QualityRevisionObject> oneOf, MetadataTag tag)
        {
            if (oneOf.TryPickT1(out QualityRevisionObject? qualityObj, out int qualityId))
            {
                return qualityObj;
            }

            string url = tag.UrlBase;
            var definitions = this.SendGetRequest<MetadataList<QualityDefinitionObject>>(url);
            if (definitions.IsError)
            {
                this.WriteError(definitions.Error);
                return null;
            }

            foreach (QualityDefinitionObject definition in definitions.Data)
            {
                PSPropertyInfo? info = definition.Properties[nameof(this.Quality)];
                if (info?.Value is QualityObject quality && quality.Id == qualityId)
                {
                    return new QualityRevisionObject(quality);
                }
            }

            return null;
        }
    }
}