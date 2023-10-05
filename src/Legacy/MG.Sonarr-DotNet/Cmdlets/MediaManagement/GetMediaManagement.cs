using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "MediaManagement", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(MediaManagement))]
    [CmdletBinding]
    public class GetMediaManagement : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/config/mediamanagement";

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord() => base.SendToPipeline(base.SendSonarrGet<MediaManagement>(EP));

        #endregion
    }
}