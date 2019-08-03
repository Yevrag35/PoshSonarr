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
    [Cmdlet(VerbsCommon.Get, "Status", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(SonarrStatusResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetStatus : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS


        #endregion

        #region PARAMETERS


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string strRes = base.TryGetSonarrResult("/system/status");

            if (!string.IsNullOrWhiteSpace(strRes))
            {
                SonarrStatusResult ssr = SonarrHttp.ConvertToSonarrResult<SonarrStatusResult>(strRes);
                ssr.UrlBase = Context.ApiCaller.BaseAddress;
                base.WriteObject(ssr);
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}