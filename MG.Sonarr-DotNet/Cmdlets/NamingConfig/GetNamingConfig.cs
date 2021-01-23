using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "NamingConfig", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(NamingConfig))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetNamingConfig : NamingConfigCmdlet
    {


        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            base.SendToPipeline(base.GetCurrentNamingConfig());
        }


        #endregion
    }
}
