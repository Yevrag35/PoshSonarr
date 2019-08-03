using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Diskspace", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(SonarrDiskspaceResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetDiskspace : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS


        #endregion

        #region PARAMETERS


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string strRes = base.TryGetSonarrResult("/diskspace");

            if (!string.IsNullOrWhiteSpace(strRes))
            {
                List<SonarrDiskspaceResult> sdr = SonarrHttp.ConvertToSonarrResults<SonarrDiskspaceResult>(strRes, out bool iso);
                base.WriteObject(sdr, true);
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}