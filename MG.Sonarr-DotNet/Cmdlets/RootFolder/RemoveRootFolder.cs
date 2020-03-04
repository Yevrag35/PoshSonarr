using MG.Posh.Extensions.Bound;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "RootFolder", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    [CmdletBinding(PositionalBinding = false, DefaultParameterSetName = "ViaPipeline")]
    public class RemoveRootFolder : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP_FORMAT = "/rootfolder/{0}";
        private List<int> _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, DontShow = true, ParameterSetName = "ViaPipeline")]
        [Alias("RootFolderId")]
        public int InputObject { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByRootFolderId")]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (this.ContainsParameter(x => x.Id))
                _ids = new List<int>(this.Id);

            else
                _ids = new List<int>();
        }

        protected override void ProcessRecord()
        {
            if (this.ContainsParameter(x => x.InputObject))
                _ids.Add(this.InputObject);
        }

        protected override void EndProcessing()
        {
            for (int i = 0; i < _ids.Count; i++)
            {
                int oneId = _ids[i];
                if (base.ShouldProcess(string.Format("RootFolderId: {0}", oneId), "Remove"))
                {
                    base.SendSonarrDelete(string.Format(EP_FORMAT, oneId));
                }
            }
        }

        #endregion
    }
}