﻿using MG.Sonarr.Results;
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
    [Cmdlet(VerbsCommon.Search, "Series", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "BySeriesName")]
    [OutputType(typeof(SeriesResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class SearchSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string SEARCH_STR_QUERY = @"term={0}";
        private const string SEARCH_ID_QUERY = @"term=tvdb:{0}";
        private bool _isStrict;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesName")]
        public string Name { get; set; }      // Each 'name' is a separate search.

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByTVDBId")]
        public long TVDBId { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "BySeriesName")]
        public SwitchParameter Strict
        {
            get => _isStrict;
            set => _isStrict = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string endpoint = this.GetEndpoint();
            List<SeriesResult> searchResults = base.SendSonarrListGet<SeriesResult>(endpoint);
            if (_isStrict)
                base.SendToPipeline(searchResults.FindAll(x => x.Name.IndexOf(this.Name, StringComparison.CurrentCultureIgnoreCase) >= 0));

            else
                base.SendToPipeline(searchResults);
        }

        #endregion

        #region BACKEND METHODS
        private string GetEndpoint()
        {
            string searchStr = null;
            if (base.HasParameterSpecified(this, x => x.Name))
            {
                searchStr = this.ParseSearchString(this.Name);
            }
            else
            {
                searchStr = this.ParseSearchId(this.TVDBId);
            }
            return string.Format(@"/series/lookup?{0}", searchStr);
        }

        private string ParseSearchString(string name)
        {
            name = name.Replace(" ", "%20");
            return string.Format(SEARCH_STR_QUERY, name);
        }
        private string ParseSearchId(long id) => string.Format(SEARCH_ID_QUERY, id);

        #endregion
    }
}