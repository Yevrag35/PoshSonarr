using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public partial class ConnectInstance
    {

        // BETA - for New-QualityDefinition
        protected override void EndProcessing()
        {
            string qStr = base.TryGetSonarrResult("/qualitydefinition");
            if (!string.IsNullOrEmpty(qStr))
            {
                Context.Qualities = SonarrHttp.ConvertToSonarrResults<QualityDefinition>(qStr, out bool iso);
            }
        }
    }
}
