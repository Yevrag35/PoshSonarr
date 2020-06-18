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
        protected override void BeginProcessing() { }
        protected override void ProcessRecord()
        {
            if (!Context.IsConnected)
            {
                base.WriteWarning("Not currently connected to any Sonarr instance.");
                return;
            }

            Context.TagManager.Dispose();
            Context.TagManager = null;
            Context.SonarrUrl = null;
            Context.ApiCaller.Dispose();
            Context.ApiCaller = null;
            //Context.AllQualities.Clear();
            Context.AllQualities = null;

            GC.Collect();
        }
    }
}
