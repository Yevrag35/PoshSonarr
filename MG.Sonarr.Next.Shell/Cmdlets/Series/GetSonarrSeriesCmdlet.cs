using MG.Sonarr.Next.Services;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models;
using MG.Sonarr.Next.Services.Models.Series;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Namotion.Reflection;
using OneOf.Types;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Get, "SonarrSeries", DefaultParameterSetName = "BySeriesName")]
    public sealed class GetSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        readonly SortedSet<int> _ids;
        readonly SortedSet<WildcardString> _names;
        MetadataTag Tag { get; }

        public GetSonarrSeriesCmdlet()
            : base()
        {
            _ids = new SortedSet<int>();
            _names = new SortedSet<WildcardString>();
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.SERIES];
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

        protected override ErrorRecord? Process()
        {
            bool hadIds = false;
            if (_ids.Count > 0)
            {
                hadIds = true;
                foreach (var result in this.GetSeriesById<SeriesObject>(_ids))
                {
                    if (result.IsError)
                    {
                        return result.Error;
                    }

                    this.WriteObject(result.Data);
                }
            }

            if (_names.Count > 0)
            {
                var response = this.GetSeriesByName<SeriesObject>(_names);
                this.WriteSonarrResults(response);
            }
            else if (!hadIds)
            {
                var response = this.GetAllSeries<SeriesObject>();
                if (response.IsError)
                {
                    return response.Error;
                }

                this.WriteCollection(response.Data);
            }

            return null;
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
                    !names.ValueLike(title))
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
                    this.WriteError(result.Error);
                    continue;
                }

                result.Data?.AddNameAlias();

                yield return result;
            }
        }
    }
}
