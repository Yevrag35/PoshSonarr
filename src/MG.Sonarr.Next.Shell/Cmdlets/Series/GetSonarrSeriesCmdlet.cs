﻿using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Shell.Output;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Get, "SonarrSeries", DefaultParameterSetName = "BySeriesName")]
    [MetadataCanPipe(Tag = Meta.CALENDAR)]
    [MetadataCanPipe(Tag = Meta.EPISODE)]
    [MetadataCanPipe(Tag = Meta.EPISODE_FILE)]
    [MetadataCanPipe(Tag = Meta.RENAMABLE)]
    [OutputType(typeof(ISeriesOutput))]
    public sealed class GetSonarrSeriesCmdlet : SonarrMetadataCmdlet
    {
        SortedSet<int> _ids = null!;
        HashSet<Wildcard> _names = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesName")]
        [SupportsWildcards]
        public IntOrString[] Name { get; set; } = Array.Empty<IntOrString>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = PSConstants.PSET_PIPELINE, DontShow = true,
            ValueFromPipeline = true)]
        [ValidateIds(ValidateRangeKind.Positive, typeof(ISeriesPipeable))]
        public ISeriesPipeable[] InputObject
        {
            get => Array.Empty<ISeriesPipeable>();
            set
            {
                if (value is not null)
                {
                    _ids ??= new();
                    _ids.UnionWith(value.Select(x => x.SeriesId));
                }
            }
        }

        protected override int Capacity => 2;
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _ids = this.GetPooledObject<SortedSet<int>>();
            _names = this.GetPooledObject<HashSet<Wildcard>>();
            var span = this.GetReturnables();
            span[0] = _ids;
            span[1] = _names;
        }
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.SERIES];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.Id);
        }
        protected override void Process(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.InputObject))
            {
                _ids.UnionWith(this.InputObject.Select(x => x.SeriesId));
            }
            else
            {
                this.Name.SplitToSets(_ids, _names);
            }

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

        private SonarrResponse<MetadataList<T>> GetSeriesByName<T>(IReadOnlySet<Wildcard> names)
            where T : PSObject, IComparable<T>, IJsonMetadataTaggable
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
            }

            return result;
        }
        private SonarrResponse<MetadataList<T>> GetAllSeries<T>()
            where T : PSObject, IComparable<T>, IJsonMetadataTaggable
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

                yield return result;
            }
        }

        bool _disposed;
        protected override void Dispose(bool disposing, IServiceScopeFactory? factory)
        {
            if (disposing && !_disposed)
            {
                _ids = null!;
                _names = null!;
                _disposed = true;
            }

            base.Dispose(disposing, factory);
        }
    }
}
