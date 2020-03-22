using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Series", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "BySeriesName")]
    [OutputType(typeof(SeriesResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetSeries : SeriesCmdlet
    {
        #region FIELDS/CONSTANTS

        private List<string> _names;
        private List<int> _ids;
        private IEqualityComparer<string> _ig;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesName")]
        [SupportsWildcards]
        public object[] Name { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySeriesId", ValueFromPipelineByPropertyName = true)]
        [Alias("SeriesId")]
        public int[] Id { get; set; }

        // EXTRA FILTERS
        [Parameter(Mandatory = false)]
        public string[] Genres { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _ig = ClassFactory.NewIgnoreCase();
            _names = new List<string>();
            _ids = new List<int>();
            if (this.ContainsParameter(x => x.Name))
                this.ProcessNamesParameter(this.Name);

            else if (this.ContainsParameter(x => x.Id))
                _ids.AddRange(this.Id);
        }

        protected override void ProcessRecord()
        {
            if (_ids.Count > 0)
            {
                base.SendToPipeline(base.GetSeriesById(_ids));
            }
            else
            {
                IEnumerable<SeriesResult> filtered = base.GetAllSeries();
                filtered = base.FilterByStrings(filtered, x => x.Name, _names.Count > 0 ? _names : null);
                
                if (this.ContainsParameter(x => x.Genres))
                {
                    filtered = filtered
                        .Where(x => 
                            this.Genres
                                .All(gen => 
                                    x.Genres
                                        .Contains(gen, _ig)));
                }

                base.SendToPipeline(filtered);
            }
        }

        #endregion

        #region BACKEND METHODS
        

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
                    if (o is IConvertible icon && int.TryParse(Convert.ToString(icon), out int outInt))
                    {
                        _ids.Add(outInt);
                    }
                    else if (o is string oStr)
                        _names.Add(oStr);
                }
            }
        }

        #endregion
    }
}