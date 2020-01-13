using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsData.Update, "Metadata", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(Metadata))]
    [CmdletBinding(PositionalBinding = false)]
    public class UpdateMetadata : MetadataCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _enabled;
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public Metadata InputObject { get; set; }

        //[Parameter(Mandatory = true, ParameterSetName = "ByMetadataId")]
        //public int Id { get; set; }

        //[Parameter(Mandatory = true, ParameterSetName = "ByMetadataName")]
        //public string Name { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            //if (base.HasParameterSpecified(this, x => x.Id))
            //    this.InputObject = base.GetMetadataById(this.Id).FirstOrDefault();

            //else if (base.HasParameterSpecified(this, x => x.Name))
            //{
            //    List<Metadata> all = base.GetAllMetadata();
            //    this.InputObject = base.GetMetadataByName(this.Name, all);
            //}
        }

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.Enabled))
                this.InputObject.IsEnabled = _enabled;

            if (base.FormatShouldProcess("Update", "Metadata Id: {0}", this.InputObject.Id))
            {
                Metadata updated = base.SendSonarrPut<Metadata>(EP, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(updated);
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}