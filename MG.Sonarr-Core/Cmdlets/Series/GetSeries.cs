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
    [Cmdlet(VerbsCommon.Get, "Series", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(SeriesResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private List<SeriesResult> _series;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0)]
        [SupportsWildcards]
        public string[] Identity { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            string jsonStr = base.TryGetSonarrResult("/series");
            if (!string.IsNullOrEmpty(jsonStr))
            {
                _series = SonarrHttpClient.ConvertToSeriesResults(jsonStr);
            }
        }

        protected override void ProcessRecord()
        {
            if (this.Identity != null && this.Identity.Length > 0)
            {
                for (int p = 0; p < this.Identity.Length; p++)
                {
                    var id = this.Identity[p];
                    var wcp = new WildcardPattern((string)id, WildcardOptions.IgnoreCase);
                    for (int s = 0; s < _series.Count; s++)
                    {
                        var series = _series[s];
                        if (wcp.IsMatch(series.Title))
                            base.WriteObject(series);
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