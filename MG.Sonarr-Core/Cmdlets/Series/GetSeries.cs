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

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesId", ValueFromPipelineByPropertyName = true)]
        public long[] SeriesId { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ParameterSetName == "BySeriesName")
            {
                try
                {
                    string jsonStr = base.TryGetSonarrResult("/series");
                    if (!string.IsNullOrEmpty(jsonStr))
                    {
                        _series = SonarrHttpClient.ConvertToSeriesResults(jsonStr);
                    }
                }
                catch (Exception e)
                {
                    base.WriteError(e, ErrorCategory.InvalidResult, "/series");
                }

                if (_series != null && _series.Count > 0 && this.Name != null && this.Name.Length > 0)
                {
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
                else if (_series != null && _series.Count > 0)
                {
                    base.WriteObject(_series, true);
                }
            }
            else
            {
                for (int i = 0; i < this.SeriesId.Length; i++)
                {
                    long id = this.SeriesId[i];
                    string full = string.Format("/series/{0}", Convert.ToString(id));
                    try
                    {
                        string oneSeries = base.TryGetSonarrResult(full);
                        if (!string.IsNullOrEmpty(oneSeries))
                        {
                            var sr = SonarrHttpClient.ConvertToSeriesResult(oneSeries);
                            base.WriteObject(sr);
                        }
                    }
                    catch (Exception e)
                    {
                        base.WriteError(e, ErrorCategory.InvalidResult, full);
                    }
                }
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}