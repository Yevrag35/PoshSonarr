using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Series;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Get, "SonarrSeries", DefaultParameterSetName = "BySeriesName")]
    public sealed class GetSonarrSeriesCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids;
        HashSet<WildcardString> _names;

        public GetSonarrSeriesCmdlet()
            : base(2)
        {
            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[0] = _ids;
            _names = this.GetPooledObject<HashSet<WildcardString>>();
            this.Returnables[1] = _names;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesName")]
        [SupportsWildcards]
        public IntOrString[] Name
        {
            get => Array.Empty<IntOrString>();
            set => value.SplitToSets(_ids, _names);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesId")]
        public int[] Id
        {
            get => Array.Empty<int>();
            set => _ids.UnionWith(value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByPipelineInput", DontShow = true,
            ValueFromPipeline = true)]
        public ISeriesPipeable[] InputObject
        {
            get => Array.Empty<ISeriesPipeable>();
            set => _ids.UnionWith(value.Select(x => x.SeriesId));
        }

        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.SERIES];
        }

        protected override void Process()
        {
            bool hadIds = false;
            if (_ids.Count > 0)
            {
                hadIds = true;
                foreach (var result in this.GetSeriesById<SeriesObject>(_ids))
                {
                    if (result.IsError)
                    {
                        this.WriteConditionalError(result.Error);
                        continue;
                    }

                    this.WriteObject(result.Data);
                }
            }

            if (_names.Count > 0)
            {
                var response = this.GetSeriesByName<SeriesObject>(_names);
                if (response.IsError)
                {
                    this.StopCmdlet(response.Error);
                    return;
                }

                this.WriteCollection(response.Data);
            }
            else if (!hadIds)
            {
                var response = this.GetAllSeries<SeriesObject>();
                if (response.IsError)
                {
                    this.StopCmdlet(response.Error);
                    return;
                }

                this.WriteCollection(response.Data);
            }
        }

        private SonarrResponse<MetadataList<T>> GetSeriesByName<T>(IReadOnlySet<WildcardString> names) where T : PSObject, IJsonMetadataTaggable
        {
            var result = this.GetAllSeries<T>();
            if (result.IsError)
            {
                return result;
            }

            for (int i = result.Data.Count - 1; i >= 0; i--)
            {
                PSObject item = result.Data[i];
                if (!item.TryGetProperty(Constants.TITLE, out string? title)
                    ||
                    !names.AnyValueLike(title))
                {
                    result.Data.RemoveAt(i);
                }
                else
                {
                    item.AddNameAlias();
                }
            }

            return result;
        }
        private SonarrResponse<MetadataList<T>> GetAllSeries<T>() where T : PSObject, IJsonMetadataTaggable
        {
            return this.SendGetRequest<MetadataList<T>>(this.Tag.UrlBase);
        }
        private IEnumerable<SonarrResponse<T>> GetSeriesById<T>(IEnumerable<int> ids) where T : PSObject, IJsonMetadataTaggable
        {
            foreach (int id in ids)
            {
                var result = this.SendGetRequest<T>($"/series/{id}");
                if (result.IsError)
                {
                    this.WriteConditionalError(result.Error);
                    continue;
                }

                result.Data?.AddNameAlias();

                yield return result;
            }
        }

        bool _disposed;
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _ids = null!;
                _names = null!;
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
