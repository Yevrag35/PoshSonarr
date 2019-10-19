﻿using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "EpisodeFile", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByEpisodeFile")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(EpisodeFile))]
    public class GetEpisodeFile : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string BASE = "/episodefile";
        private const string EP_BY_SERIES = BASE + "?seriesId={0}";
        private const string EP_BY_EP = BASE + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByEpisodeFileInput")]
        public EpisodeFile EpisodeFile { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByEpisodeFileId", ValueFromPipelineByPropertyName = true)]
        public long EpisodeFileId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "BySeriesId", ValueFromPipeline = true)]
        public SeriesResult Series { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string full = this.ParameterSetName == "BySeriesId"
                ? string.Format(EP_BY_SERIES, this.Series.SeriesId)
                : this.ParameterSetName == "ByEpisodeFileId"
                    ? string.Format(EP_BY_EP, this.EpisodeFileId)
                    : string.Format(EP_BY_EP, this.EpisodeFile.EpisodeFileId);

            string jsonStr = base.TryGetSonarrResult(full);

            if (!string.IsNullOrEmpty(jsonStr))
            {
                List<EpisodeFile> result = SonarrHttp.ConvertToSonarrResults<EpisodeFile>(jsonStr, out bool iso);
                base.WriteObject(result, true);
            }
        }

        #endregion

        #region METHODS


        #endregion
    }
}