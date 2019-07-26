using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Remove, "Series", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    public class RemoveSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS


        #endregion

        #region PARAMETERS

        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public long SeriesId { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter DeleteFiles { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            bool delete = this.MyInvocation.BoundParameters.ContainsKey("DeleteFiles")
                ? this.DeleteFiles.ToBool()
                : false;

            string apiUri = string.Format("/series/{0}?deleteFiles={1}", this.SeriesId, delete.ToString().ToLower());

            if (this.Force || base.ShouldProcess(string.Format("Series Id: {0}", this.SeriesId), "Delete"))
            {
                base.TryDeleteSonarrResult(apiUri);
            }
        }

        #endregion
    }
}