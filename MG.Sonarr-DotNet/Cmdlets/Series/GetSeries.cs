using MG.Sonarr.Functionality;
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

        private List<SeriesResult> _allSeries;
        private List<string> _names;
        private List<long> _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesName")]
        [SupportsWildcards]
        public object[] Name{ get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesId", ValueFromPipelineByPropertyName = true)]
        [Alias("SeriesId")]
        public long[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _names = new List<string>();
            _ids = new List<long>();
            if (base.HasParameterSpecified(this, x => x.Name))
                this.ProcessNamesParameter(this.Name);

            else if (base.HasParameterSpecified(this, x => x.Id))
                _ids.AddRange(this.Id);
        }

        protected override void ProcessRecord()
        {
            if (_ids.Count > 0)
            {
                base.SendToPipeline(this.GetSeriesById(_ids));
            }
            else
            {
                List<SeriesResult> all = base.SendSonarrListGet<SeriesResult>(EP);
                base.SendToPipeline(base.FilterByStrings(all, x => x.Name, _names.Count > 0 ? _names : null));
            }
        }

        #endregion

        #region BACKEND METHODS
        private IEnumerable<SeriesResult> GetSeriesById(IEnumerable<long> ids)
        {
            foreach (long id in ids)
            {
                SeriesResult sr = base.SendSonarrGet<SeriesResult>(string.Format(EP_BY_ID, id));
                if (sr != null)
                    yield return sr;
            }
        }

        private void ProcessNamesParameter(object[] objNames)
        {
            if (this.MyInvocation.Line.IndexOf(" -Name ", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                foreach (IConvertible ic in objNames)
                {
                    _names.Add(Convert.ToString(ic));
                }
            }
            else
            {
                for (int i = 0; i < objNames.Length; i++)
                {
                    object o = objNames[i];
                    if (o is IConvertible icon && long.TryParse(Convert.ToString(icon), out long outLong))
                    {
                        _ids.Add(outLong);
                    }
                    else if (o is string oStr)
                        _names.Add(oStr);
                }
            }
        }

        #endregion
    }
}