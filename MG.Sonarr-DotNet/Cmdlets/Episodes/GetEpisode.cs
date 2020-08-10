using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Episode", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "NotId")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(EpisodeResult))]
    public class GetEpisode : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private EpisodeIdentifier _identifier;

        private bool _dled;
        private bool _hasAired;
        private HashSet<long> _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, ValueFromPipeline = true, ParameterSetName = "NotId")]
        public SeriesResult[] InputObject { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "NotId")]
        public int[] SeriesId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByEpisodeId")]
        [Alias("EpisodeId")]
        public long[] Id
        {
            get => _ids.ToArray();
            set => _ids = new HashSet<long>(value);
        }

        [Parameter(Mandatory = false, ParameterSetName = "NotId")]
        public int[] AbsoluteEpisodeNumber { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "NotId")]
        public object[] EpisodeIdentifier { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "NotId")]
        public SwitchParameter Downloaded
        {
            get => _dled;
            set => _dled = value;
        }

        [Parameter(Mandatory = false, ParameterSetName = "NotId")]
        public SwitchParameter HasAired
        {
            get => _hasAired;
            set => _hasAired = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (this.ContainsParameter(x => x.EpisodeIdentifier))
            {
                if (this.EpisodeIdentifier.Count(x => x is int || x is long) <= 0)
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
                else
                {
                    _ids = new HashSet<long>(this.EpisodeIdentifier.OfType<long>());
                    _ids.UnionWith(this.CastIdsToLong(this.EpisodeIdentifier.OfType<int>()));

                    this.MyInvocation.BoundParameters.Add("Id", this.Id);
                    this.MyInvocation.BoundParameters.Remove("EpisodeIdentifier");
                }
            }
            else if (this.ContainsParameter(x => x.AbsoluteEpisodeNumber))
            {
                _identifier = new Sonarr.EpisodeIdentifier();
                _identifier.AbsoluteEpisodes.UnionWith(this.AbsoluteEpisodeNumber);
            }
        }

        protected override void ProcessRecord()
        {
            Endpoint ep = Endpoint.Episode;
            var epList = new HashSet<EpisodeResult>();
            if (this.ContainsParameter(x => x.Id))
            {
                this.GetEpisodeById(_ids, epList);
            }
            else
            {
                if (this.ContainsParameter(x => x.InputObject))
                    this.SeriesId = this.InputObject.Select(x => x.Id).ToArray();

                List<EpisodeResult> allEps = new List<EpisodeResult>(24);
                foreach (int sid in this.SeriesId)
                {
                    allEps.AddRange(base.SendSonarrListGet<EpisodeResult>(ep.WithQuery(EpisodeResult.GetSeriesIdParameter(sid))));
                }

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
            IEnumerable<EpisodeResult> ers = epList.OrderBy(x => x, comparer);
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
        private IEnumerable<long> CastIdsToLong(IEnumerable<int> ints)
        {
            foreach (int i in ints)
            {
                yield return Convert.ToInt64(i);
            }
        }
        private void GetEpisodeById(IEnumerable<long> epIds, ISet<EpisodeResult> addToSet)
        {
            foreach (long id in epIds)
            {
                EpisodeResult epRes = base.SendSonarrGet<EpisodeResult>(Endpoint.Episode.WithId(id));
                if (epRes != null)
                    addToSet.Add(epRes);
            }
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