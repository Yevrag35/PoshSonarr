using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "HostConfig", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(UIHost))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetHostConfig : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/config/host";

        #endregion

        #region PARAMETERS


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            UIHost uiConfig = base.SendSonarrGet<UIHost>(EP);
            base.SendToPipeline(uiConfig);
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}