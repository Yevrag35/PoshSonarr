using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommunications.Disconnect, "Instance", ConfirmImpact = ConfirmImpact.None)]
    [Alias("Disconnect-")]
    public class DisconnectInstance : BaseSonarrCmdlet
    {
        private bool _passThru;

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        protected override void BeginProcessing() { }
        protected override void ProcessRecord()
        {
            if (!Context.IsConnected)
            {
                base.WriteWarning("Nothing to do; not connected to any Sonarr instance.");
                return;
            }

            base.WriteObject(Context.Disinitialize(_passThru));

            GC.Collect();
        }
    }
}
