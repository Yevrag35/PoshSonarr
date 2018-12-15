using MG.Api;
using Sonarr.Api.Results;
using System.Collections.Generic;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets.Base
{
    public abstract class BaseCmdlet : PSCmdlet
    {
        internal ApiCaller Api
        {
            get => SonarrServiceContext.TheCaller;
            set => SonarrServiceContext.TheCaller = value;
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (!SonarrServiceContext.IsSet)
                throw new SonarrContextNotSetException("  Run the 'Connect-Sonarr' cmdlet first.");
        }
    }
}
