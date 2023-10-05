using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Status", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(Status))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetStatus : BaseSonarrCmdlet
    {
        private const string EP = "/system/status";

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord() => base.SendToPipeline(base.SendSonarrGet<Status>(EP));

        #endregion
    }
}