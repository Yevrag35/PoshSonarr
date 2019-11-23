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
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "BySeriesId")]
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "BySeriesIdAbsoluteEp")]
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "BySeriesIdSeasonEp")]
        public long SeriesId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByEpisodeId", ValueFromPipelineByPropertyName = true)]
        public long EpisodeId { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesIdAbsoluteEp")]
        public int[] AbsoluteEpisodeNumber { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesIdSeasonEp")]
        public EpisodeIdentifier[] EpisodeIdentifier { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string full = this.ParameterSetName != "ByEpisodeId" 
                ? string.Format(EP_BY_SERIES, this.SeriesId) 
                : string.Format(EP_BY_EP, this.EpisodeId);

            string jsonStr = base.TryGetSonarrResult(full);

            if (!string.IsNullOrEmpty(jsonStr))
            {
                List<EpisodeResult> result = SonarrHttp.ConvertToSonarrResults<EpisodeResult>(jsonStr, out bool iso);
                foreach (EpisodeResult er in result)
                {
                    if (er.AirDateUtc.HasValue)
                    {
                        er.AirDateUtc = er.AirDateUtc.Value.ToUniversalTime();
                    }
                }
                if (this.MyInvocation.BoundParameters.ContainsKey("AbsoluteEpisodeNumber"))
                {
                    IEnumerable<EpisodeResult> results = result.Where(x => x.AbsoluteEpisodeNumber.HasValue && this.AbsoluteEpisodeNumber.Contains(x.AbsoluteEpisodeNumber.Value));
                    base.WriteObject(results, true);
                }

                else if (this.MyInvocation.BoundParameters.ContainsKey("EpisodeIdentifier"))
                {
                    var list = new List<EpisodeResult>(result.Count);
                    for (int i = 0; i < this.EpisodeIdentifier.Length; i++)
                    {
                        EpisodeIdentifier epid = this.EpisodeIdentifier[i];
                        for (int e = 0; e < result.Count; e++)
                        {
                            EpisodeResult er = result[e];
                            if (er.SeasonNumber == epid.Season)
                            {
                                if (!epid.Episode.HasValue || (epid.Episode.HasValue && er.EpisodeNumber == epid.Episode.Value))
                                    list.Add(er);
                            }
                        }
                    }
                    //list.Sort(new EpisodeComparer());
                    list.Sort();
                    //var ieq = new EpisodeEquality();
                    //base.WriteObject(list.Distinct(ieq), true);
                    base.WriteObject(list.Distinct(), true);
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