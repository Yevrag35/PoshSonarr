using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Strings;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

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
        //private EpisodeIdentifierCollection _epIdCol;
        private EpisodeIdentifier _identifier;

        private bool _dled;
        private bool _hasAired;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, DontShow = true, ValueFromPipeline = true, ParameterSetName = "ByInputAbsoluteEp")]
        [Parameter(Mandatory = true, DontShow = true, ValueFromPipeline = true, ParameterSetName = "ByInputSeasonEp")]
        public SeriesResult InputObject { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "BySeriesIdAbsoluteEp")]
        [Parameter(Mandatory = true, ParameterSetName = "BySeriesIdSeasonEp")]
        public int SeriesId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByEpisodeId")]
        [Alias("EpisodeId")]
        public int Id { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesIdAbsoluteEp")]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByInputAbsoluteEp")]
        public int[] AbsoluteEpisodeNumber { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesIdSeasonEp")]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByInputSeasonEp")]
        public object[] EpisodeIdentifier { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "ByInputAbsoluteEp")]
        [Parameter(Mandatory = false, ParameterSetName = "ByInputSeasonEp")]
        [Parameter(Mandatory = false, ParameterSetName = "BySeriesIdAbsoluteEp")]
        [Parameter(Mandatory = false, ParameterSetName = "BySeriesIdSeasonEp")]
        public SwitchParameter Downloaded
        {
            get => _dled;
            set => _dled = value;
        }

        [Parameter(Mandatory = false, ParameterSetName = "ByInputAbsoluteEp")]
        [Parameter(Mandatory = false, ParameterSetName = "ByInputSeasonEp")]
        [Parameter(Mandatory = false, ParameterSetName = "BySeriesIdAbsoluteEp")]
        [Parameter(Mandatory = false, ParameterSetName = "BySeriesIdSeasonEp")]
        public SwitchParameter HasAired
        {
            get => _hasAired;
            set => _hasAired = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            if (this.ContainsParameter(x => x.EpisodeIdentifier))
            {
                _identifier = Sonarr.EpisodeIdentifier.Parse(this.EpisodeIdentifier);
                if (_identifier.AbsoluteSeasons.Count <= 0 && _identifier.AbsoluteEpisodes.Count <= 0 && _identifier.SeasonEpisodePairs.Count <= 0)
                {
                    base.ThrowTerminatingError(new ErrorRecord(new ArgumentException(
                        "An invalid episode identifier was supplied, so no seasons or episodes were parsed."),
                        typeof(ArgumentException).FullName,
                        ErrorCategory.InvalidArgument,
                        this.EpisodeIdentifier));
                }
            }
            else if (this.ContainsParameter(x => x.AbsoluteEpisodeNumber))
            {
                _identifier = new Sonarr.EpisodeIdentifier();
                _identifier.AbsoluteEpisodes.UnionWith(this.AbsoluteEpisodeNumber);
            }


            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            var epList = new HashSet<EpisodeResult>();
            if (this.ContainsParameter(x => x.Id))
            {
                this.GetEpisodeById(this.Id, epList);
            }
            else
            {
                if (this.ContainsParameter(x => x.InputObject))
                    this.SeriesId = this.InputObject.Id;

                List<EpisodeResult> allEps = base.SendSonarrListGet<EpisodeResult>(
                    string.Format(ApiEndpoints.Episode_SeriesId, this.SeriesId)
                );

                if (_identifier != null)
                {
                    this.GetEpisodeByIdentifier(_identifier, allEps, ref epList);
                }
                else
                {
                    epList.UnionWith(allEps);
                }
            }
            IComparer<EpisodeResult> comparer = EpisodeResult.GetComparer();
            IEnumerable<EpisodeResult> ers = epList.Distinct().OrderBy(x => x, comparer);
            if (this.ContainsParameter(x => x.Downloaded))
            {
                ers = ers.Where(x => x.IsDownloaded == _dled);
            }

            if (this.ContainsParameter(x => x.HasAired))
            {
                ers = ers.Where(x => x.HasAired == _hasAired);
            }

            base.WriteObject(ers, true);
        }

        #endregion

        #region METHODS
        private void GetEpisodeById(long epId, ISet<EpisodeResult> addToSet)
        {
            string uri = string.Format(EP_BY_EP, epId);
            EpisodeResult epRes = base.SendSonarrGet<EpisodeResult>(uri);
            if (epRes != null)
                addToSet.Add(epRes);
        }
        private void GetEpisodeByIdentifier(EpisodeIdentifier identifier, IList<EpisodeResult> allEpisodes, ref HashSet<EpisodeResult> addToSet)
        {
            for (int i = 0; i < allEpisodes.Count; i++)
            {
                EpisodeResult er = allEpisodes[i];
                if (identifier.FallsInRange(er))
                    addToSet.Add(er);
            }
        }

        #endregion
    }
}