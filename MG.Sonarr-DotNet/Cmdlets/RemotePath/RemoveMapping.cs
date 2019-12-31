using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "Mapping", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    [CmdletBinding(PositionalBinding = false)]
    public class RemoveMapping : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP_FORMAT = "/remotepathmapping/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("MappingId")]
        public int Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.ShouldProcess(string.Format("Remote Path Mapping Id: {0}", this.Id), "Remove"))
            {
                base.SendSonarrDelete(string.Format(EP_FORMAT, this.Id));
            }
        }

        #endregion
    }
}