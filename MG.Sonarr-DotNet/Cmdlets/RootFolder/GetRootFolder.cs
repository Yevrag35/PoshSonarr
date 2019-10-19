using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "RootFolder", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(RootFolder))]
    public class GetRootFolder : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/rootfolder";
        private const string EP_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0)]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.MyInvocation.BoundParameters.ContainsKey("Id"))
            {
                for (int i = 0; i < this.Id.Length; i++)
                {
                    string ep = string.Format(EP_ID, this.Id[i]);
                    string jsonRes = base.TryGetSonarrResult(ep);
                    if (!string.IsNullOrEmpty(jsonRes))
                    {
                        RootFolder res = SonarrHttp.ConvertToSonarrResult<RootFolder>(jsonRes);
                        base.WriteObject(res);
                    }
                }
            }
            else
            {
                string jsonRes = base.TryGetSonarrResult(EP);
                if (!string.IsNullOrEmpty(jsonRes))
                {
                    List<RootFolder> reses = SonarrHttp.ConvertToSonarrResults<RootFolder>(jsonRes, out bool iso);
                    base.WriteObject(reses, true);
                }
            }
        }

        #endregion

        #region METHODS


        #endregion
    }
}