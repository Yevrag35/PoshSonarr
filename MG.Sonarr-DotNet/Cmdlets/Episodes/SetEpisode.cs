using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Set, "Episode", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
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
            this.InputObject.Monitored = this.IsMonitored;

            if (base.ShouldProcess(
                string.Format(
                    "Episode #{0} IsMonitored set to {1}; Series {2}", this.InputObject.AbsoluteEpisodeNumber, this.IsMonitored, this.InputObject.SeriesId), 
                "Set"))
            {
                string jsonBody = this.InputObject.ToJson();
                string jsonRes = base.TryPutSonarrResult(EP, jsonBody);
                if (!string.IsNullOrEmpty(jsonRes) && _passThru)
                {
                    EpisodeResult res = SonarrHttp.ConvertToSonarrResult<EpisodeResult>(jsonRes);
                    base.WriteObject(res);
                }
            }
        }

        #endregion

        #region METHODS


        #endregion
    }
}