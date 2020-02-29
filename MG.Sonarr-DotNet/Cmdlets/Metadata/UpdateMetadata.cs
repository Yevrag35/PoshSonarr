using MG.Posh.Extensions.Bound;
using MG.Sonarr.Results;
using System;
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
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ContainsParameter(x => x.Enabled))
            {
                this.InputObject.IsEnabled = _enabled;
            }

            if (base.FormatShouldProcess("Update", "Metadata Id: {0}", this.InputObject.Id))
            {
                Metadata updated = base.SendSonarrPut<Metadata>(EP, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(updated);
            }
        }

        #endregion
    }
}