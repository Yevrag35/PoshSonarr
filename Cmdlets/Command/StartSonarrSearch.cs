using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "SonarrSearch", SupportsShouldProcess = true)]
    public class StartSonarrSearch : NonPipeableCommandCmdlet
    {
        private const string WARNING = "Waiting for a '{0}' can take a long time. Make sure the 'TimeOut' is set appropriately.";
        private const string BY_SERIES_INPUT = "BySeriesInput";
        private const string BY_EPISODE_INPUT = "ByEpisodeInput";
        private const string BY_EPISODE_IDS = "ByEpisodeIds";
        private const string MISSING_EPISODES = "MissingEpisodesSearch";
        private const string EPISODE_IDS = "episodeIds";
        private const string SERIES_ID = "seriesId";
        private const string SEASON_NUMBER = "seasonNumber";

        private SonarrCommand _cmd;
        private bool _isEp => ParameterSetName != BY_SERIES_INPUT;
        internal override SonarrCommand Command => _cmd;

        private List<long> ParameterValue;

        #region SEARCH PARAMETERS

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = BY_SERIES_INPUT)]
        public SeriesResult Series { get; set; }

        [Parameter(Mandatory = false, Position = 1, ParameterSetName = BY_SERIES_INPUT)]
        public int SeasonNumber { get; set; }

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = BY_EPISODE_INPUT)]
        public EpisodeResult Episode { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = BY_EPISODE_IDS)]
        public long[] EpisodeId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = MISSING_EPISODES)]
        public SwitchParameter MissingEpisodesSearch { get; set; }

        #endregion

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            ParameterValue = new List<long>();
        }

        protected override void ProcessRecord() => _cmd = GetSonarrCommand();

        protected override void EndProcessing()
        {
            if (!MyInvocation.BoundParameters.ContainsKey(MISSING_EPISODES))
            {
                IDictionary parameters;
                if (!_isEp)
                {
                    if (WaitForCompletion)
                    {
                        var warningText = string.Format(WARNING, _cmd.ToString());
                        WriteWarning(warningText);
                    }

                    for (int i = 0; i < ParameterValue.Count; i++)
                    {
                        var p = ParameterValue[i];
                        if (Force || ShouldContinue("Perform '" + _cmd.ToString() + "' on seriesId " + Convert.ToString(p), "Are you sure?"))
                        {
                            parameters = GetParameters(_isEp, p);
                            var result = ProcessCommand(parameters, _cmd);
                            WriteObject(result, true);
                        }
                    }
                }
                else if (Force || ShouldContinue("Perform '" + _cmd.ToString() + "' on episodeIds: " + string.Join(", ", ParameterValue), "Are you sure?"))
                {
                    parameters = GetParameters(_isEp, ParameterValue);
                    var result = ProcessCommand(parameters, _cmd);
                    WriteObject(result, true);
                }
            }
            else
            {
                var result = ProcessCommand(null, _cmd);
                WriteObject(result);
            }
        }

        private SonarrCommand GetSonarrCommand()
        {
            switch (ParameterSetName)
            {
                case BY_SERIES_INPUT:
                    // Series Search or Season Search
                    ParameterValue.Add(Series.Id);
                    return !MyInvocation.BoundParameters.ContainsKey("SeasonNumber") ? 
                        SonarrCommand.SeriesSearch : SonarrCommand.SeasonSearch;

                case MISSING_EPISODES:
                    return SonarrCommand.missingEpisodeSearch;

                default:
                    if (MyInvocation.BoundParameters.ContainsKey("Episode"))
                        ParameterValue.Add(Episode.Id);
                    else
                        ParameterValue.AddRange(EpisodeId);
                    return SonarrCommand.EpisodeSearch;
            }
        }

        private IDictionary GetParameters(bool isEpId, object paramValue)
        {
            var parameters = new Dictionary<string, object>();
            if (!isEpId)
            {
                parameters.Add(SERIES_ID, paramValue);
                if (MyInvocation.BoundParameters.ContainsKey("SeasonNumber"))
                    parameters.Add(SEASON_NUMBER, SeasonNumber);
            }
            else
                parameters.Add(EPISODE_IDS, paramValue);

            return parameters;
        }
    }
}
