using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Gets any download client in Sonarr.</para>
    /// <para type="description">Retrieves any/all of the download clients that have been created in Sonarr.</para>
    /// <para type="description">By default, all clients are returned.</para>
    /// <para type="description">You optionally specify to return clients by their download protocol or their ID's.</para>
    /// </summary>
    /// <example>
    ///     <code>Get-SonarrDownloadClient Torrent</code>
    /// </example>
    /// <example>
    ///     <code>Get-SonarrDownloadClient -Protocol Usenet, Torrent</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "DownloadClient", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByProtocol")]
    [OutputType(typeof(DownloadClient))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetDownloadClient : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        //private const string EP = "/downloadclient";
        //private const string EP_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        /// <summary>
        /// <para type="description">Specifies to only retrive download clients whose download protocol match the given value(s).</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProtocol")]
        public DownloadProtocol[] Protocol { get; set; }

        /// <summary>
        /// <para type="description">Retrieve the download client by their IDs.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ByClientId", Position = 0)]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if ( ! this.ContainsParameter(x => x.Id))
            {
                List<DownloadClient> clients = this.GetAllDownloadClients();
                
                if ( ! this.ContainsParameter(x => x.Protocol))
                    base.SendToPipeline(clients);

                else
                    base.SendToPipeline(this.FindByProtocol(clients));
            }
            else
            {
                for (int i = 0; i < this.Id.Length; i++)
                {
                    DownloadClient dlc = base.SendSonarrGet<DownloadClient>(string.Format(ApiEndpoints.DownloadClient_ById, this.Id[i]));
                    base.SendToPipeline(dlc);
                }
            }
        }

        #endregion

        #region BACKEND METHODS
        private List<DownloadClient> GetAllDownloadClients()
        {
            return base.SendSonarrListGet<DownloadClient>(ApiEndpoints.DownloadClient);
        }

        private List<DownloadClient> FindByProtocol(List<DownloadClient> list) => 
            list
                .FindAll(x => this.Protocol.Contains(x.Protocol));

        #endregion
    }
}