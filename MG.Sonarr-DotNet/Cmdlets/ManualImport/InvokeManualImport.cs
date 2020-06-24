using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Invoke, "ManualImport", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [OutputType(typeof(CommandResult))]
    public class InvokeManualImport : BaseManualImportCmdlet
    {
        #region FIELDS/CONSTANTS
        private List<ManualImport> _toDo;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public ManualImport[] InputObject { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _toDo = new List<ManualImport>();
        }

        protected override void ProcessRecord()
        {
            _toDo.AddRange(this.InputObject);
        }

        protected override void EndProcessing()
        {

        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}