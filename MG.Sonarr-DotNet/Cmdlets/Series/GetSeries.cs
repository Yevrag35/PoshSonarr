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
    public sealed class GetSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private List<SeriesResult> _series;
        //private bool _showAll = false;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesName")]
        [SupportsWildcards]
        public string[] Name{ get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesId", ValueFromPipelineByPropertyName = true)]
        public long[] SeriesId { get; set; }

        //[Parameter(Mandatory = false, DontShow = true)]
        //public SwitchParameter DebugShowAll
        //{
        //    get => _showAll;
        //    set => _showAll = value;
        //}

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ParameterSetName == "BySeriesName")
            {
                _series = base.SendSonarrListGet<SeriesResult>("/series");

                if (_series != null && _series.Count > 0 && this.Name != null && this.Name.Length > 0)
                {
                    base.WriteObject(this.FilterByName(), true);
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
                    string full = string.Format("/series/{0}", id);
                    SeriesResult sr = base.SendSonarrGet<SeriesResult>(full);
                    base.WriteObject(sr);
                }
            }
        }

        #endregion

        #region BACKEND METHODS
        private IEnumerable<SeriesResult> FilterByName()
        {
            for (int i = 0; i < this.Name.Length; i++)
            {
                string name = this.Name[i];
                var wcp = new WildcardPattern(name, WildcardOptions.IgnoreCase);
                for (int s = 0; s < _series.Count; s++)
                {
                    SeriesResult series = _series[s];
                    if (wcp.IsMatch(series.Name))
                        yield return series;
                }
            }
        }

        #endregion
    }
}