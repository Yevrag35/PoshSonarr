using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsData.Update, "DownloadClient", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(DownloadClient))]
    [CmdletBinding]
    public class UpdateDownloadClient : BaseSonarrCmdlet
    {
        private bool _force;
        private bool _passThru;

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public DownloadClient InputObject { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        protected override void BeginProcessing() => base.BeginProcessing();
        protected override void ProcessRecord()
        {
            base.WriteDebug(this.InputObject.ToJson());
            if (_force || base.FormatShouldProcess("Update", "Download Client Id: {0}", this.InputObject.Id))
            {
                DownloadClient modified = base.SendSonarrPut<DownloadClient>(this.InputObject.GetEndpoint(), this.InputObject);
                if (_passThru)
                    base.SendToPipeline(modified);
            }
        }
    }
}
