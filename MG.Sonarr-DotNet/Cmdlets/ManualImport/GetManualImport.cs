using MG.Posh.Extensions.Bound;
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
        private List<ManualImport> _list;
        private bool _noRejects;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("FullName")]
        public string[] Path { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        [SupportsWildcards]
        public string[] SeriesName { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter NoRejections
        {
            get => _noRejects;
            set => _noRejects = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _list = new List<ManualImport>();
        }

        protected override void ProcessRecord()
        {
            foreach (string importPath in this.Path)
            {
                List<ManualImport> mis = base.GetPossibleImports(importPath);
                if (mis != null && mis.Count > 0)
                    _list.AddRange(base.FilterByStrings(mis, x => x.Series, this.SeriesName));
            }
        }

        protected override void EndProcessing()
        {
            if (this.ContainsParameter(x => x.NoRejections))
            {
                base.SendToPipeline(_list.FindAll(
                    x => (x.Rejections.Length <= 0) == _noRejects));
            }
            else
                base.SendToPipeline(_list);
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}