using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Set, "Episode", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [Alias("Update-Episode")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(EpisodeResult))]
    public class SetEpisode : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/episode";
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public EpisodeResult InputObject { get; set; }

        [Parameter(Mandatory = true, Position = 0)]
        public bool IsMonitored { get; set; }

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
            this.InputObject.IsMonitored = this.IsMonitored;

            if (base.ShouldProcess(
                string.Format(
                    "Episode #{0} IsMonitored set to {1}; Series {2}", 
                    this.InputObject.AbsoluteEpisodeNumber, 
                    this.IsMonitored, 
                    this.InputObject.SeriesId),
                    "Set"))
            {
                EpisodeResult er = base.SendSonarrPut<EpisodeResult>(EP, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(er);
            }
        }

        #endregion
    }
}