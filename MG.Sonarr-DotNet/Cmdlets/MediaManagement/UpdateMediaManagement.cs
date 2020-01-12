using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsData.Update, "MediaManagement", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(MediaManagement))]
    [CmdletBinding(PositionalBinding = false)]
    public class UpdateMediaManagement : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/config/mediamanagement";
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public MediaManagement InputObject { get; set; }

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
            if (base.ShouldProcess("Global Media Management", "Update"))
            {
                MediaManagement mm = base.SendSonarrPut<MediaManagement>(EP, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(mm);
            }
        }

        #endregion
    }
}