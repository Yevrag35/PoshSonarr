using MG.Api;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrRootFolder", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    public class GetSonarrRootFolder : BaseCmdlet
    {
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            
        }
    }
}
