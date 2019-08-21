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
    [Cmdlet(VerbsCommon.Get, "Mapping", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByRemotePathName")]
    [OutputType(typeof(RemotePathMapping))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetMapping : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/remotepathmapping";
        private const string EP_WITH_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, ParameterSetName = "ByMappingId")]
        public int[] MappingId { get; set; }

        [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByRemotePathName")]
        [SupportsWildcards]
        [Alias("FullName")]
        public string RemotePath { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ParameterSetName == "ByRemotePathName")
            {
                string jsonRes = base.TryGetSonarrResult(EP);
                if (!string.IsNullOrEmpty(jsonRes))
                {
                    List<RemotePathMapping> mappings = SonarrHttp.ConvertToSonarrResults<RemotePathMapping>(jsonRes);
                    if (this.MyInvocation.BoundParameters.ContainsKey("RemotePath"))
                    {
                        var wcp = new WildcardPattern(this.RemotePath);
                        base.WriteObject(mappings.FindAll(x => wcp.IsMatch(x.RemotePath)), true);
                    }
                    else
                        base.WriteObject(mappings, true);
                }
            }
            else if (this.MyInvocation.BoundParameters.ContainsKey("MappingId"))
            {
                for (int i = 0; i < this.MappingId.Length; i++)
                {
                    string jsonRes = base.TryGetSonarrResult(string.Format(EP_WITH_ID, this.MappingId[i]));
                    if (!string.IsNullOrEmpty(jsonRes))
                    {
                        RemotePathMapping mapping = SonarrHttp.ConvertToSonarrResult<RemotePathMapping>(jsonRes);
                        base.WriteObject(mapping);
                    }
                }
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}