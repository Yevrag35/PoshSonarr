using MG.Http.Urls.Queries;
using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Remove, "SonarrSeries", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [Alias("Delete-SonarrSeries")]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    public sealed class RemoveSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        static readonly IQueryParameter FALSE = QueryParameter.Create("deleteFiles", false);
        static readonly IQueryParameter TRUE = QueryParameter.Create("deleteFiles", true);

        QueryParameterCollection _col = null!;
        SortedDictionary<int, string?> _dict = null!;
        MetadataTag Tag { get; set; } = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id
        {
            get => Array.Empty<int>();
            set
            {
                _dict ??= new();
                foreach (int id in value)
                {
                    _ = _dict.TryAdd(id, null);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE,
            DontShow = true)]
        [ValidateNotNull]
        [ValidateIds(ValidateRangeKind.Positive)]
        public SeriesObject[] InputObject
        {
            get => Array.Empty<SeriesObject>();
            set
            {
                _dict ??= new();
                AddIdsToDict(value, _dict);
            }
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false)]
        public SwitchParameter DeleteFiles
        {
            get => SwitchParameter.Present;
            set
            {
                _col ??= new(1);
                if (value.ToBool())
                {
                    _col.Remove(FALSE.Key);
                    _col.Add(TRUE);
                }
            }
        }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.SERIES];
        }

        private static void AddIdsToDict(IEnumerable<SeriesObject> array, IDictionary<int, string?> dictionary)
        {
            foreach (SeriesObject so in array)
            {
                _ = dictionary.TryAdd(so.Id, so.Title);
            }
        }

        protected override void End(IServiceProvider provider)
        {
            if (_dict.Count <= 0)
            {
                this.WriteWarning("No series were passed via the pipeline. Make sure to pass the correct object type.");
                this.StopCmdlet();
                return;
            }

            bool force = this.Force.ToBool();
            bool noToAll = false;
            bool yesToAll = false;

            foreach (var kvp in _dict)
            {
                string url = GetUrl(this.Tag, kvp.Key, _col);

                if (this.ShouldProcess(url, "Deleting Series")
                    &&
                    (force
                    ||
                    this.ShouldContinue(in kvp, ref yesToAll, ref noToAll)))
                {
                    this.SendDeleteSeries(url);
                }
            }
        }

        private bool ShouldContinue(in KeyValuePair<int, string?> kvp, ref bool yesToAll, ref bool noToAll)
        {
            return this.ShouldContinue(
                    query: GetQueryMessage(in kvp),
                    caption: $"Delete Series ID: {kvp.Key}",
                    yesToAll: ref yesToAll,
                    noToAll: ref noToAll);
        }
        private void SendDeleteSeries(string url)
        {
            var result = this.SendDeleteRequest(url);
            if (result.IsError)
            {
                if (result.Error.IsIgnorable)
                {
                    this.WriteWarning(result.Error.Message);
                }
                else
                {
                    this.WriteConditionalError(result.Error);
                }
            }
        }
        private static string GetQueryMessage(in KeyValuePair<int, string?> kvp)
        {
            object id = !string.IsNullOrWhiteSpace(kvp.Value)
                ? $"{kvp.Value} ({kvp.Key})"
                : kvp.Key;

            return $"Are you sure you want to delete the Series \"{id}\"?";
        }

        private static string GetUrl(MetadataTag tag, int id, QueryParameterCollection col)
        {
            return tag.GetUrlForId(id, col);
        }
    }
}
