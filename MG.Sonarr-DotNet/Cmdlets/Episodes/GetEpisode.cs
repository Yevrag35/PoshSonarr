using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Episode", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "BySeriesId")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(EpisodeResult))]
    public class GetEpisode : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string BASE = "/episode";
        private const string EP_BY_SERIES = BASE + "?seriesId={0}";
        private const string EP_BY_EP = BASE + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "BySeriesId")]
        public long SeriesId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByEpisodeId", ValueFromPipelineByPropertyName = true)]
        public long EpisodeId { get; set; }

        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "BySeriesId")]
        public int[] EpisodeNumber { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string full = this.ParameterSetName == "BySeriesId" 
                ? string.Format(EP_BY_SERIES, this.SeriesId) 
                : string.Format(EP_BY_EP, this.EpisodeId);

            string jsonStr = base.TryGetSonarrResult(full);

            if (!string.IsNullOrEmpty(jsonStr))
            {
                var result = SonarrHttpClient.ConvertToSonarrResults<EpisodeResult>(jsonStr, out bool iso);
                foreach (EpisodeResult er in result)
                {
                    if (er.AirDateUtc.HasValue)
                    {
                        er.AirDateUtc = er.AirDateUtc.Value.ToUniversalTime();
                    }
                }
                if (this.MyInvocation.BoundParameters.ContainsKey("EpisodeNumber"))
                {
                    var results = result.Where(x => this.EpisodeNumber.Contains(x.EpisodeNumber));
                    base.WriteObject(results, true);
                }
                
                else
                    base.WriteObject(result, true);
            }
        }

        #endregion

        #region METHODS


        #endregion
    }
}