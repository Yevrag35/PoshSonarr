using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Strings;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Invoke, "ManualImport", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [OutputType(typeof(CommandOutput))]
    public class InvokeManualImport : BaseManualImportCmdlet
    {
        #region FIELDS/CONSTANTS
        private List<ManualImport> _toDo;
        private bool _force;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public ManualImport[] InputObject { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _toDo = new List<ManualImport>();
        }

        protected override void ProcessRecord()
        {
            _toDo.AddRange(this.CheckEach(this.InputObject));
        }

        protected override void EndProcessing()
        {
            if (_toDo.Count > 0)
            {
                if (_force || base.FormatShouldProcess("Import", "{0} manual import(s)", _toDo.Count))
                {
                    SonarrBodyParameters sbp = ManualImportPost.NewManualImportObject(_toDo);
                    CommandOutput cr = base.SendSonarrPost<CommandOutput>(ApiEndpoints.Command, sbp);
                    if (cr != null)
                    {
                        base.WriteObject(cr);
                        History.Jobs.AddResult(cr);
                    }
                }
            }
        }

        #endregion

        #region BACKEND METHODS
        private IEnumerable<ManualImport> CheckEach(IEnumerable<ManualImport> each)
        {
            foreach (ManualImport mi in each)
            {
                if (mi.Rejections.Length > 0 && !mi.IsReadyToPost())
                {
                    base.WriteWarning(string.Format("{0} has not been properly identified.  Skipping...", mi.Id));
                }
                else
                {
                    yield return mi;
                }
            }
        }

        #endregion
    }
}