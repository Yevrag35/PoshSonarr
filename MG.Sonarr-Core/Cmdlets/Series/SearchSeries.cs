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
    [Cmdlet(VerbsCommon.Search, "Series", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true, DefaultParameterSetName = "BySeriesName")]
    //[OutputType(typeof())]
    [CmdletBinding(PositionalBinding = false)]
    public class SearchSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string SEARCH_STR_QUERY = @"term={0}";
        private const string SEARCH_ID_QUERY = @"term=tvdb:{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesName")]
        public string Name { get; set; }      // Each 'name' is a separate search.

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByTVDBId")]
        public long TVDBId { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "BySeriesName")]
        public SwitchParameter Strict { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ParameterSetName == "BySeriesName")
            {
                List<SeriesResult> list = null;
                string searchStr = this.ParseSearchString(this.Name);
                string full = string.Format(@"/series/lookup?{0}", searchStr);

                if (base.ShouldProcess(full, "Executing API call"))
                {
                    try
                    {
                        string jsonStr = base.TryGetSonarrResult(full);

                        if (!string.IsNullOrEmpty(jsonStr))
                        {
                            var tok = JToken.Parse(jsonStr);
                            list = SonarrHttpClient.ConvertToSeriesResults(jsonStr, false);
                            if (this.Strict.ToBool())
                                base.WriteObject(list.FindAll(x => x.Title.Contains(this.Name, StringComparison.CurrentCultureIgnoreCase)), true);

                            else
                                base.WriteObject(list, true);
                        }
                    }
                    catch (Exception e)
                    {
                        base.WriteError(e, ErrorCategory.InvalidResult, full);
                    }
                }
            }
            else
            {
                    string searchStr = this.ParseSearchId(this.TVDBId);
                    string full = string.Format(@"/series/lookup?{0}", searchStr);

                    if (base.ShouldProcess(full, "Executing API call"))
                    {
                        try
                        {
                            string jsonStr = base.TryGetSonarrResult(full);

                            if (!string.IsNullOrEmpty(jsonStr))
                            {
                                var tok = JToken.Parse(jsonStr);
                                var list = SonarrHttpClient.ConvertToSeriesResults(jsonStr, false);
                                base.WriteObject(list, true);
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
        private ProgressRecord NewProgressRecord(int on, int total, string name)
        {
            return new ProgressRecord(0, "Series Lookup", string.Format("Searching for series {0}/{1}... {2}", on, total, name));
        }

        private string ParseSearchString(string name)
        {
            name = name.Replace(" ", "%20");
            return string.Format(SEARCH_STR_QUERY, name);
        }
        private string ParseSearchId(long id) => string.Format(SEARCH_ID_QUERY, id);

        #endregion

        private class IgnoreCase : IEqualityComparer<string>
        {
            public bool Equals(string x, string y) => x.Equals(y, StringComparison.CurrentCultureIgnoreCase);
            public int GetHashCode(string obj) => obj.GetHashCode();
        }
    }
}