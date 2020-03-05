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
    [Cmdlet(VerbsCommon.Get, "EpisodeFile", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByEpisodeFileId")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(EpisodeFile))]
    public sealed class GetEpisodeFile : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string BASE = "/episodefile";
        private const string EP_BY_SERIES = BASE + "?seriesId={0}";
        private const string EP_BY_EP = BASE + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true, ParameterSetName = "ByEpisodeFileInput")]
        public EpisodeResult Episode { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByEpisodeFileId", Position = 0)]
        public int Id { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "BySeriesId", ValueFromPipeline = true)]
        public SeriesResult Series { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string fullEndpoint = this.GetEndpointString(out bool isSingular);
            if (isSingular)
            {
                base.SendToPipeline(base.SendSonarrGet<EpisodeFile>(fullEndpoint));
            }
            else
            {
                base.SendToPipeline(base.SendSonarrListGet<EpisodeFile>(fullEndpoint), true);
            }
        }

        #endregion

        #region METHODS
        private string GetEndpointString(out bool isSingular)
        {
            isSingular = false;
            if (this.ContainsParameter(x => x.Series))
            {
                return string.Format(EP_BY_SERIES, this.Series.Id);
            }
            else if (this.ContainsParameter(x => x.Id))
            {
                isSingular = true;
                return string.Format(EP_BY_EP, this.Id);
            }
            else
            {
                isSingular = true;
                return string.Format(EP_BY_EP, this.Episode.EpisodeFile.Id);
            }
        }

        #endregion
    }
}