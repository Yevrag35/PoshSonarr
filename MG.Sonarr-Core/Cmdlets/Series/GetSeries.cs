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
        private List<SeriesResult> _series;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesName")]
        [SupportsWildcards]
        public string[] Name{ get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesId")]
        public long[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ParameterSetName == "BySeriesName")
            {
                string jsonStr = base.TryGetSonarrResult("/series");
                if (!string.IsNullOrEmpty(jsonStr))
                {
                    _series = SonarrHttpClient.ConvertToSeriesResults(jsonStr);
                }

                for (int p = 0; p < this.Name.Length; p++)
                {
                    string id = this.Name[p];
                    var wcp = new WildcardPattern((string)id, WildcardOptions.IgnoreCase);
                    for (int s = 0; s < _series.Count; s++)
                    {
                        SeriesResult series = _series[s];
                        if (wcp.IsMatch(series.Title))
                            base.WriteObject(series);
                    }
                }
            }
            else if (this.Id != null && this.Id.Length > 0)
            {
                for (int i = 0; i < this.Id.Length; i++)
                {
                    long id = this.Id[i];
                    string oneSeries = base.TryGetSonarrResult(string.Format("/series/{0}", Convert.ToString(id)));
                    if (!string.IsNullOrEmpty(oneSeries))
                    {
                        var sr = SonarrHttpClient.ConvertToSeriesResult(oneSeries);
                        base.WriteObject(sr);
                    }
                }
            }
            else
            {
                base.WriteObject(_series, true);
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}