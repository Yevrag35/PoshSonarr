using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public class NamingConfigCmdlet : BaseSonarrCmdlet
    {
        protected private const string EP = "/config/naming";

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        #endregion

        #region BACKEND METHODS
        protected NamingConfig GetCurrentNamingConfig()
        {
            return base.SendSonarrGet<NamingConfig>(EP);
        }

        #endregion
    }
}
