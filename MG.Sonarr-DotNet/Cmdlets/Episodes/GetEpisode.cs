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
        private EpisodeIdentifierCollection _epIdCol;

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
        public string[] EpisodeIdentifier { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            if (this.MyInvocation.BoundParameters.ContainsKey("EpisodeIdentifier"))
                _epIdCol = this.EpisodeIdentifier;

            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            var epList = new List<EpisodeResult>();
            if (this.MyInvocation.BoundParameters.ContainsKey("EpisodeId"))
            {
                this.GetEpisodeById(this.EpisodeId, epList);
            }
            else
            {
                List<EpisodeResult> allEps = base.SendSonarrListGet<EpisodeResult>(string.Format(EP_BY_SERIES, this.SeriesId));

                if (this.MyInvocation.BoundParameters.ContainsKey("EpisodeIdentifier"))
                {
                    this.GetEpisodeByIdentifierString(_epIdCol, allEps, epList);
                }
                else if (this.MyInvocation.BoundParameters.ContainsKey("AbsoluteEpisodeNumber"))
                {
                    this.GetEpisodeByAbsoluteNumber(this.AbsoluteEpisodeNumber, allEps, epList);
                }
                else
                {
                    epList.AddRange(allEps);
                }
            }
            epList.Sort();
            base.WriteObject(epList.Distinct(), true);
        }

        #endregion

        #region METHODS
        private void GetEpisodeByAbsoluteNumber(int[] absoluteIds, List<EpisodeResult> allEpisodes, List<EpisodeResult> addToList)
        {
            addToList
                .AddRange(allEpisodes
                    .FindAll(x => this.AbsoluteEpisodeNumber
                        .Contains(x.AbsoluteEpisodeNumber
                            .GetValueOrDefault())));
        }
        private void GetEpisodeById(long epId, List<EpisodeResult> addToList)
        {
            string uri = string.Format(EP_BY_EP, epId);
            EpisodeResult epRes = base.SendSonarrGet<EpisodeResult>(uri);
            if (epRes != null)
                addToList.Add(epRes);
        }

        private void GetEpisodeByIdentifierString(EpisodeIdentifierCollection idCol, List<EpisodeResult> allEpisodes, List<EpisodeResult> addToList)
        {
            for (int i = 0; i < allEpisodes.Count; i++)
            {
                EpisodeResult er = allEpisodes[i];
                if (idCol.AnyMatchesEpisode(er))
                    addToList.Add(er);
            }
        }

        #endregion
    }
}