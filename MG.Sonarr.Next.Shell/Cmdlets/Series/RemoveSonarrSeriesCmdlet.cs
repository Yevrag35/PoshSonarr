using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Remove, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [Alias("Delete-SonarrSeries")]
    [CmdletBinding(PositionalBinding = false, ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class RemoveSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        readonly SortedDictionary<int, string?> _dict;
        MetadataTag Tag { get; }

        public RemoveSonarrSeriesCmdlet()
            : base()
        {
            _dict = new();
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.SERIES];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
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
        public object[] InputObject
        {
            get => Array.Empty<object>();
            set => AddIdsToSet(value, _dict);
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter DeleteFiles { get; set; }

        private static void AddIdsToSet(object[]? array, IDictionary<int, string?> dictionary)
        {
            if (array is null)
            {
                return;
            }

            foreach (object item in array)
            {
                if (item.IsCorrectType(Meta.SERIES, out PSObject? pso)
                    &&
                    pso.TryGetProperty(Constants.ID, out int id))
                {
                    _ = dictionary.TryAdd(id, pso.TryGetProperty(Constants.TITLE, out string? title)
                        ? title
                        : null);
                }
            }
        }

        protected override ErrorRecord? End()
        {
            if (_dict.Count <= 0)
            {
                this.WriteWarning("No series were passed via the pipeline. Make sure to pass the correct object type.");
                return null;
            }

            bool force = this.Force.ToBool();
            bool noToAll = false;
            bool yesToAll = false;

            foreach (var kvp in _dict)
            {
                string url = GetUrl(this.Tag, kvp.Key, this.DeleteFiles);
                if (this.ShouldProcess(url, "Deleting Series")
                    &&
                    (force
                    ||
                    this.ShouldContinue(in kvp, ref yesToAll, ref noToAll)))
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
                            this.WriteError(result.Error);
                        }
                    }
                }
            }

            return null;
        }

        private bool ShouldContinue(in KeyValuePair<int, string?> kvp, ref bool yesToAll, ref bool noToAll)
        {
            return this.ShouldContinue(
                    query: GetQueryMessage(in kvp),
                    caption: $"Delete Series ID: {kvp.Key}",
                    yesToAll: ref yesToAll,
                    noToAll: ref noToAll);
        }
        private static string GetQueryMessage(in KeyValuePair<int, string?> kvp)
        {
            object id = !string.IsNullOrWhiteSpace(kvp.Value)
                ? $"{kvp.Value} ({kvp.Key})"
                : kvp.Key;

            return $"Are you sure you want to delete the Series \"{id}\"?";
        }

        private static string GetUrl(MetadataTag tag, int id, SwitchParameter deleteFiles)
        {
            QueryParameterCollection parameters = new()
            {
                { nameof(deleteFiles), deleteFiles.ToBool() }
            };

            return tag.GetUrlForId(id, parameters);
        }
    }
}
