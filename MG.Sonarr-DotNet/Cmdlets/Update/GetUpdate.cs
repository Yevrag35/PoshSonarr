using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Update", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(Update))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetUpdate : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/update";
        private bool _older;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false)]
        public SwitchParameter IncludeOlder
        {
            get => _older;
            set => _older = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string strRes = base.TryGetSonarrResult(EP);
            
            if (!string.IsNullOrWhiteSpace(strRes))
            {
                List<Update> ups = this.ConvertToUpdates(strRes);
                if (ups.Count > 0)
                {
                    if (!_older)
                        base.WriteObject(ups.Single(x => x.Latest));

                    else
                        base.WriteObject(ups, true);
                }
            }
        }

        #endregion

        #region BACKEND METHODS
        private List<Update> ConvertToUpdates(string jsonResult) => SonarrHttp.ConvertToSonarrResults<Update>(jsonResult, out bool iso);

        #endregion
    }
}