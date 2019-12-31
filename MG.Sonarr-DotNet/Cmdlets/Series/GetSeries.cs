using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Series", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "BySeriesName")]
    [OutputType(typeof(SeriesResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/series";
        private const string EP_BY_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesName")]
        [SupportsWildcards]
        public string[] Name{ get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesId", ValueFromPipelineByPropertyName = true)]
        [Alias("SeriesId")]
        public long[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if ( ! base.HasParameterSpecified(this, x => x.Id))
                base.SendToPipeline(this.ProcessBySeriesName());
            
            else
            {
                foreach (long sid in this.Id)
                {
                    string full = string.Format(EP_BY_ID, sid);
                    SeriesResult sr = base.SendSonarrGet<SeriesResult>(full);
                    base.SendToPipeline(sr);
                }
            }
        }

        #endregion

        #region BACKEND METHODS
        private List<SeriesResult> ProcessBySeriesName()
        {
            List<SeriesResult> allSeries = base.SendSonarrListGet<SeriesResult>(EP);
            return base.FilterByStringParameter(allSeries, x => x.Name, this, c => c.Name);
        }

        #endregion
    }
}