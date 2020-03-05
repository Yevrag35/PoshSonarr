using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsData.Update, "Notification", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(Notification))]
    [CmdletBinding(PositionalBinding = false)]
    public class UpdateNotification : NotificationCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public Notification InputObject { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.FormatShouldProcess("Update", "Notification Id: {0}", this.InputObject.Id))
            {
                Notification updated = base.SendSonarrPut<Notification>(this.Endpoint, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(updated);
            }
        }

        #endregion
    }
}