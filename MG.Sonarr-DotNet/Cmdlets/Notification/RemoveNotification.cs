using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "Notification", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true, DefaultParameterSetName = "ViaPipeline")]
    [Alias("Remove-Connection")]
    [CmdletBinding(PositionalBinding = false)]
    public class RemoveNotification : NotificationCmdlet
    {
        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ViaPipeline")]
        public Notification InputObject { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ById")]
        public int Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.InputObject))
                this.Id = this.InputObject.Id;

            if (base.FormatShouldProcess("Remove", "Notification Connection Id: {0}", this.Id))
                base.SendSonarrDelete(base.FormatWithId(this.Id));
        }

        #endregion
    }
}