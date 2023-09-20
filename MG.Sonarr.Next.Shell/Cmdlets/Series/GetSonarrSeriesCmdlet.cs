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
        MetadataResolver Resolver { get; }

        public GetSonarrSeriesCmdlet()
            : base()
        {
            _ids = new SortedSet<int>();
            _names = new SortedSet<WildcardString>();
            this.Resolver = this.Services.GetRequiredService<MetadataResolver>();
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
                    this.WriteSonarrResult(result);
                }
            }

            if (_names.Count > 0)
            {
                var response = this.GetSeriesByName(_names);
                this.WriteSonarrResult(response);
            }
            else if (!hadIds)
            {
                var response = this.GetAllSeries();
                this.WriteSonarrResult(response);
            }

            return null;
        }

        private SonarrResponse<List<SonarrObject>> GetSeriesByName(IReadOnlySet<WildcardString> names)
        {
            var result = this.GetAllSeries();
            if (result.IsError)
            {
                return result;
            }

            for (int i = result.Data.Count - 1; i >= 0; i--)
            {
                object item = result.Data[i];
                if (!item.TryGetProperty(Constants.TITLE, out string? title)
                    ||
                    !names.ValueLike(title))
                {
                    result.Data.RemoveAt(i);
                }
                else
                {
                    item.AddNameAlias();
                    bool added = this.Resolver.AddToObject(Meta.SERIES, item);
                    Debug.Assert(added);
                }
            }

            return result;
        }
        private SonarrResponse<List<SonarrObject>> GetAllSeries()
        {
            //return this.Client.SendGetAsync<List<object>>("/series").GetAwaiter().GetResult();
            return this.SendGetRequest<List<SonarrObject>>(Meta.SERIES);
        }
        private IEnumerable<SonarrResponse<SonarrObject>> GetSeriesById(IEnumerable<int> ids)
        {
            MetadataTag tag = this.Resolver[Meta.SERIES];
            foreach (int id in ids)
            {
                var result = this.SendGetRequest<SonarrObject>($"/series/{id}");
                if (result.IsError)
                {
                    this.WriteError(result.Error);
                    continue;
                }
                //var result = this.Client.SendGetAsync<object>($"/series/{id}").GetAwaiter().GetResult();
                result.Data?.AddNameAlias();
                if (result.Data is not null)
                {
                    result.Data.Metadata = tag;
                }
                //bool added = this.Resolver.AddToObject(Meta.SERIES, result.Data!);
                //Debug.Assert(added);

                yield return result;
            }
        }
    }
}
