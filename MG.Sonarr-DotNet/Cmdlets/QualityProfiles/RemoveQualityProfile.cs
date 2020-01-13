using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "QualityProfile", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [CmdletBinding]
    public class RemoveQualityProfile : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/profile";
        private const string EP_WITH_ID = EP + "/{0}";

        private bool _force;
        private HashSet<int> _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true, DontShow = true, ParameterSetName = "ViaPipeline")]
        public QualityProfile InputObject { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByProfileId")]
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
            if (base.HasParameterSpecified(this, x => x.Id))
                _ids = new HashSet<int>(this.Id);

            else
                _ids = new HashSet<int>();
        }

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.InputObject))
                _ids.Add(this.InputObject.Id);
        }

        protected override void EndProcessing()
        {
            foreach (int id in _ids)
            {
                if (_force || base.FormatShouldProcess("Remove", "Quality Profile Id: {0}", id))
                {
                    base.SendSonarrDelete(string.Format(EP_WITH_ID, id));
                }
            }
        }

        #endregion
    }
}