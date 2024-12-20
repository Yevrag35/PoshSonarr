using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Remove, "SonarrSeries", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [Alias("Delete-SonarrSeries")]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    public sealed class RemoveSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        QueryCol _col = null!;
        SortedDictionary<int, string?> _dict = null!;
        MetadataTag Tag { get; set; } = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false)]
        public SwitchParameter DeleteFiles { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        [DistinctValues(typeof(int))]
        public int[] Id { get; set; } = [];

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE, DontShow = true)]
        [ValidateNotNull]
        [ValidateIds(ValidateRangeKind.Positive)]
        public SeriesObject[] InputObject { get; set; } = [];

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        protected override int Capacity => 2;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.SERIES];

            _dict = this.GetPooledObject<SortedDictionary<int, string?>>();
            _col = this.GetPooledObject<QueryCol>();
            this.SetReturnables(_dict, _col);
        }

        private static void AddIdsToDict(ReadOnlySpan<SeriesObject> array, SortedDictionary<int, string?> dictionary)
        {
            foreach (SeriesObject obj in array)
            {
                if (!dictionary.ContainsKey(obj.Id))
                {
                    dictionary.Add(obj.Id, obj.Title);
                }
            }
        }
        private static void AddIdsToDict(ReadOnlySpan<int> ids, SortedDictionary<int, string?> dictionary)
        {
            foreach (ref readonly int id in ids)
            {
                if (!dictionary.ContainsKey(id))
                {
                    dictionary.Add(id, null);
                }
            }
        }

        protected override void Process(IServiceProvider provider)
        {
            if (this.Id.Length > 0)
            {
                AddIdsToDict(this.Id, _dict);
            }

            if (this.InputObject.Length > 0)
            {
                AddIdsToDict(this.InputObject, _dict);
            }
        }

        protected override void End(IServiceProvider provider)
        {
            if (_dict.Count == 0)
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

        private static string GetUrl(MetadataTag tag, int id, QueryCol queryCollection)
        {
            return tag.GetUrlForId(id, queryCollection);
        }
    }
}
