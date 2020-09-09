using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsData.Update, "QualityProfile", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [Alias("Set-QualityProfile", "Update-Profile", "Set-Profile")]
    [OutputType(typeof(QualityProfile))]
    [CmdletBinding(PositionalBinding = false)]
    public class UpdateQualityProfile : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _force;
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public QualityProfile InputObject { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
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
            if (_force || base.FormatShouldProcess("Update", "QualityProfile Id: {0}", this.InputObject.Id))
            {
                string ep = this.InputObject.GetEndpoint();
                QualityProfile updated = base.SendSonarrPut<QualityProfile>(ep, this.InputObject);
            }
        }

        #endregion
    }
}