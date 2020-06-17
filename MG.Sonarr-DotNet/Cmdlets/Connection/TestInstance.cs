//using MG.Sonarr.Extensions;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsDiagnostic.Test, "Instance")]
    [Alias("Test-")]
    public class TestInstance : BaseSonarrCmdlet
    {


        protected override void BeginProcessing() { }

        protected override void ProcessRecord()
        {
            base.WriteObject(Context.GetConnectionStatus());
        }

        protected override void EndProcessing() { }
    }
}
