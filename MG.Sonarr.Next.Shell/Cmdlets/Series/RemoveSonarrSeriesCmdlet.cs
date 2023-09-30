using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Series;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Remove, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [Alias("Delete-SonarrSeries")]
    [CmdletBinding(PositionalBinding = false, ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class RemoveSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        static readonly IQueryParameter FALSE = QueryParameter.Create("deleteFiles", false);
        static readonly IQueryParameter TRUE = QueryParameter.Create("deleteFiles", true);

        readonly QueryParameterCollection _col;
        readonly SortedDictionary<int, string?> _dict;
        MetadataTag Tag { get; }

        public RemoveSonarrSeriesCmdlet()
            : base()
        {
            _col = new()
            {
                FALSE,
            };
            _dict = new();
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.SERIES];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id
        {
            get => Array.Empty<int>();
            set
            {
                foreach (int id in value)
                {
                    _ = _dict.TryAdd(id, null);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput", DontShow = true)]
        [ValidateNotNull]
        public SeriesObject[] InputObject
        {
            get => Array.Empty<SeriesObject>();
            set => AddIdsToDict(value, _dict);
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
                if (value.ToBool())
                {
                    _col.Remove(FALSE.Key);
                    _col.Add(TRUE);
                }
            }
        }

        private static void AddIdsToDict(IEnumerable<SeriesObject> array, IDictionary<int, string?> dictionary)
        {
            foreach (SeriesObject so in array)
            {
                _ = dictionary.TryAdd(so.Id, so.Title);
            }
        }

        protected override void End()
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

                if (this.ShouldProcess(url, $"Deleting Series")
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
