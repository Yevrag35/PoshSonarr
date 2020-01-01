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
    [Cmdlet(VerbsCommon.Get, "Diskspace", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "None")]
    [OutputType(typeof(DiskspaceResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetDiskspace : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/diskspace";

        private List<string> _stringArgs;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ParameterSetName = "ViaPathPipeline", ValueFromPipelineByPropertyName = true, DontShow = true)]
        public string InputPath { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByExplicitPath", Position = 0)]
        [SupportsWildcards]
        public string[] Path { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByLabel")]
        [SupportsWildcards]
        public string[] Label { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (base.HasParameterSpecified(this, x => x.Path))
                _stringArgs = new List<string>(this.Path);

            else if (base.HasParameterSpecified(this, x => x.Label))
                _stringArgs = new List<string>(this.Label);

            else
                _stringArgs = new List<string>();
        }
        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.InputPath))
                _stringArgs.Add(this.InputPath);
        }
        protected override void EndProcessing()
        {
            List<DiskspaceResult> allResults = base.SendSonarrListGet<DiskspaceResult>(EP);
            base.SendToPipeline(this.Filter(allResults));
        }

        private List<DiskspaceResult> Filter(List<DiskspaceResult> diskspaceResults)
        {
            if (base.HasAnyParameterSpecified(this, x => x.Path, x => x.InputPath, x => x.Label))
                //return base.FilterByStrings(diskspaceResults, dsr => dsr.Path, _stringArgs);
                return base.FilterByMultipleStrings(diskspaceResults, _stringArgs, x => x.Path, x => x.Label);

            else
                return diskspaceResults;
        }

        #endregion
    }
}