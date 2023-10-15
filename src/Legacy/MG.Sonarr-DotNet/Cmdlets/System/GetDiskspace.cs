using MG.Posh.Extensions.Bound;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Diskspace", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByExplicitPath")]
    [OutputType(typeof(Diskspace))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetDiskspace : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/diskspace";
        private bool _showMost;
        //private string[] _path;
        //private string[] _labl;

        private List<string> _stringArgs = new List<string>();

        #endregion

        #region PARAMETERS

        [Parameter(Mandatory = false, ParameterSetName = "ByExplicitPath", Position = 0)]
        [SupportsWildcards]
        public string[] Path
        {
            get => null;
            set => _stringArgs.AddRange(value);
        }

        [Parameter(Mandatory = true, ParameterSetName = "ByLabel")]
        [SupportsWildcards]
        public string[] Label
        {
            get => null;
            set => _stringArgs.AddRange(value);
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter ShowMostFreespace
        {
            get => _showMost;
            set => _showMost = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();
        protected override void ProcessRecord()
        {
            List<Diskspace> allResults = base.SendSonarrListGet<Diskspace>(EP);
            List<Diskspace> filtered = this.Filter(allResults);
            if (_showMost)
            {
                base.SendToPipeline(filtered.
                    Find(x =>
                        filtered
                            .Max(dr =>
                                dr.FreeSpace.GetValueOrDefault()) == x.FreeSpace.GetValueOrDefault()));
            }
            else
            {
                base.SendToPipeline(filtered);
            }
        }

        private List<Diskspace> Filter(List<Diskspace> diskspaceResults)
        {
            if (this.ContainsParameter(x => x.Path))
                return base.FilterByMultipleStrings(diskspaceResults, _stringArgs, x => x.Path);

            else if (this.ContainsParameter(x => x.Label))
                return base.FilterByMultipleStrings(diskspaceResults, _stringArgs, x => x.Label);

            else
                return diskspaceResults;
        }

        #endregion
    }
}