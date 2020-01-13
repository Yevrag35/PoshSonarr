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
    [Cmdlet(VerbsCommon.Get, "Status", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(StatusResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetStatus : BaseSonarrCmdlet
    {
        private const string EP = "/system/status";

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord() => base.SendToPipeline(base.SendSonarrGet<StatusResult>(EP));

        #endregion
    }
}