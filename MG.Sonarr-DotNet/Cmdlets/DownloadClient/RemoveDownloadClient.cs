using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality.Strings;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "DownloadClient", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [CmdletBinding()]
    public class RemoveDownloadClient : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _force;
        private HashSet<int> _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ParameterSetName = "ViaPipeline", ValueFromPipeline = true)]
        public DownloadClient InputObject { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByExplicitIds", Position = 0)]
        public int[] Id { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (this.ContainsParameter(x => x.Id))
            {
                _ids = new HashSet<int>(this.Id);
            }
            else
            {
                _ids = new HashSet<int>();
            }
        }

        protected override void ProcessRecord()
        {
            if (this.ContainsParameter(x => x.InputObject))
            {
                _ids.Add(this.InputObject.Id);
            }
        }

        protected override void EndProcessing()
        {
            foreach (int id in _ids)
            {
                if (_force || base.FormatShouldProcess("Remove", "Download Client Id: {0}", id))
                {
                    base.SendSonarrDelete(string.Format(ApiEndpoints.DownloadClient_ById, id));
                }
            }
        }

        #endregion
    }
}