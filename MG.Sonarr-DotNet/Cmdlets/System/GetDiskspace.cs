using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Diskspace", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByExplicitPath")]
    [OutputType(typeof(DiskspaceResult))]
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
            List<DiskspaceResult> allResults = base.SendSonarrListGet<DiskspaceResult>(EP);
            List<DiskspaceResult> filtered = this.Filter(allResults);
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

        private List<DiskspaceResult> Filter(List<DiskspaceResult> diskspaceResults)
        {
            if (base.HasParameterSpecified(this, x => x.Path))
                return base.FilterByMultipleStrings(diskspaceResults, _stringArgs, x => x.Path);

            else if (base.HasParameterSpecified(this, x => x.Label))
                return base.FilterByMultipleStrings(diskspaceResults, _stringArgs, x => x.Label);

            else
                return diskspaceResults;
        }

        #endregion
    }
}