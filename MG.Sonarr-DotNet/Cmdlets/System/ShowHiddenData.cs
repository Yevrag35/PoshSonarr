using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using Microsoft.PowerShell.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Show, "HiddenData")]
    public class ShowHiddenData : PSCmdlet
    {
        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public IAdditionalInfo[] InputObject { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
        }

        protected override void ProcessRecord()
        {
            foreach (IAdditionalInfo obj in this.InputObject)
            {
                IDictionary addInfo = obj.GetAdditionalInfo();
                JObject job = JObject.FromObject(addInfo);
                object formatted = JsonObject.ConvertFromJson(job.ToString(), out ErrorRecord errRec);

                if (errRec != null)
                {
                    errRec.ErrorDetails = new ErrorDetails("Unable to convert the additional info from JSON.");
                    errRec.ErrorDetails.RecommendedAction = "Tuck your head between knees and kiss your ass goodbye...";
                    base.WriteError(errRec);
                }
                else if (formatted != null)
                {
                    base.WriteObject(formatted, true);
                }
            }
        }

        protected override void EndProcessing()
        {
        }

        #endregion
    }
}