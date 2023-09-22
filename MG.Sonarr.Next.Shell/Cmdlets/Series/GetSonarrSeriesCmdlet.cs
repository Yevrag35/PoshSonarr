using MG.Sonarr.Next.Services;
using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
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
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesId",
            ValueFromPipelineByPropertyName = true)]
        [Alias("SeriesId")]
        public int[] Id
        {
            get => Array.Empty<int>();
            set => _ids.UnionWith(value);
        }

        protected override ErrorRecord? Process()
        {
            bool hadIds = false;
            if (_ids.Count > 0)
            {
                hadIds = true;
                foreach (var result in this.GetSeriesById(_ids))
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
                var response = this.GetSeriesByName(_names);
                this.WriteSonarrResult(response);
            }
            else if (!hadIds)
            {
                var response = this.GetAllSeries(addMetadata: true);
                if (response.IsError)
                {
                    return response.Error;
                }

                this.WriteCollection(response.Data);
            }

            return null;
        }

        private SonarrResponse<List<PSObject>> GetSeriesByName(IReadOnlySet<WildcardString> names)
        {
            var result = this.GetAllSeries();
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
                    item.AddMetadata(this.Tag);
                }
            }

            return result;
        }
        private SonarrResponse<List<PSObject>> GetAllSeries(bool addMetadata = false)
        {
            var list = this.SendGetRequest<List<PSObject>>(this.Tag.UrlBase);
            if (addMetadata && !list.IsError)
            {
                foreach (var item in list.Data)
                {
                    item.AddMetadata(this.Tag);
                }
            }

            return list;
        }
        private IEnumerable<SonarrResponse<PSObject>> GetSeriesById(IEnumerable<int> ids)
        {
            foreach (int id in ids)
            {
                var result = this.SendGetRequest<PSObject>($"/series/{id}");
                if (result.IsError)
                {
                    this.WriteError(result.Error);
                    continue;
                }

                result.Data?.AddNameAlias();
                result.Data?.AddMetadata(this.Tag);

                yield return result;
            }
        }
    }
}
