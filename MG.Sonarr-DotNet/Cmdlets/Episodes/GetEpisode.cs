using MG.Posh.Extensions.Bound;
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
    [Cmdlet(VerbsCommon.Get, "Episode", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByInputSeasonEp")]
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
        [Parameter(Mandatory = true, DontShow = true, ValueFromPipeline = true, ParameterSetName = "ByInputAbsoluteEp")]
        [Parameter(Mandatory = true, DontShow = true, ValueFromPipeline = true, ParameterSetName = "ByInputSeasonEp")]
        public SeriesResult InputObject { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "BySeriesIdAbsoluteEp")]
        [Parameter(Mandatory = true, ParameterSetName = "BySeriesIdSeasonEp")]
        public long SeriesId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByEpisodeId")]
        public long EpisodeId { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesIdAbsoluteEp")]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByInputAbsoluteEp")]
        public int[] AbsoluteEpisodeNumber { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesIdSeasonEp")]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByInputSeasonEp")]
        public string[] EpisodeIdentifier { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            if (this.ContainsParameter(x => x.EpisodeIdentifier))
                _epIdCol = this.EpisodeIdentifier;

            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            var epList = new List<EpisodeResult>();
            if (this.ContainsParameter(x => x.EpisodeId))
            {
                this.GetEpisodeById(this.EpisodeId, epList);
            }
            else
            {
                if (this.ContainsParameter(x => x.InputObject))
                    this.SeriesId = this.InputObject.Id;

                List<EpisodeResult> allEps = base.SendSonarrListGet<EpisodeResult>(string.Format(EP_BY_SERIES, this.SeriesId));

                if (this.ContainsParameter(x => x.EpisodeIdentifier))
                {
                    this.GetEpisodeByIdentifierString(_epIdCol, allEps, ref epList);
                }
                else if (this.ContainsParameter(x => x.AbsoluteEpisodeNumber))
                {
                    this.GetEpisodeByAbsoluteNumber(this.AbsoluteEpisodeNumber, allEps, ref epList);
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
        private void GetEpisodeByAbsoluteNumber(int[] absoluteIds, List<EpisodeResult> allEpisodes, ref List<EpisodeResult> addToList)
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
        private void GetEpisodeByIdentifierString(EpisodeIdentifierCollection idCol, List<EpisodeResult> allEpisodes, ref List<EpisodeResult> addToList)
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