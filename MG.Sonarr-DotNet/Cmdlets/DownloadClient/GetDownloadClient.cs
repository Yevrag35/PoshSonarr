using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DownloadClient", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByProtocol")]
    [OutputType(typeof(DownloadClient))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetDownloadClient : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/downloadclient";
        private const string EP_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProtocol")]
        public DownloadProtocol[] Protocol { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByClientId")]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ParameterSetName != "ByClientId")
            {
                List<DownloadClient> clients = this.GetAllDownloadClients();
                if (clients != null)
                {
                    if (!this.MyInvocation.BoundParameters.ContainsKey("Protocol"))
                        base.WriteObject(clients, true);

                    else
                        base.WriteObject(this.FindByProtocol(clients), true);
                }
            }
            else
            {
                for (int i = 0; i < this.Id.Length; i++)
                {
                    string jsonRes = base.TryGetSonarrResult(string.Format(EP_ID, this.Id[i]));
                    if (!string.IsNullOrEmpty(jsonRes))
                    {
                        DownloadClient dlCli = SonarrHttp.ConvertToSonarrResult<DownloadClient>(jsonRes);
                        base.WriteObject(dlCli);
                    }
                }
            }
        }

        #endregion

        #region BACKEND METHODS
        private List<DownloadClient> GetAllDownloadClients()
        {
            string jsonRes = base.TryGetSonarrResult(EP);
            return !string.IsNullOrEmpty(jsonRes) 
                ? SonarrHttp.ConvertToSonarrResults<DownloadClient>(jsonRes, out bool iso) 
                : null;
        }

        private List<DownloadClient> FindByProtocol(List<DownloadClient> list) => list.FindAll(x => this.Protocol.Contains(x.Protocol));

        #endregion
    }
}