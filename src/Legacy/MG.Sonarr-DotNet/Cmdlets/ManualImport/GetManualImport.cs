using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "ManualImport")]
    [OutputType(typeof(ManualImport))]
    public class GetManualImport : BaseManualImportCmdlet
    {
        #region FIELDS/CONSTANTS


        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("FullName")]
        public string[] Path { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            foreach (string importPath in this.Path)
            {
                var list = base.GetPossibleImports(importPath);
                base.SendToPipeline(list);
            }
        }

        protected override void EndProcessing()
        {

        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}