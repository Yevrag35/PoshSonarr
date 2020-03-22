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
        private bool _noTags;
        private List<int> _ids;
        private bool _isMon;
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

        [Parameter(Mandatory = false)]
        public SwitchParameter HasNoTags
        {
            get => _noTags;
            set => _noTags = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter IsMonitored
        {
            get => _isMon;
            set => _isMon = value;
        }

        [Parameter(Mandatory = false)]
        public float MaximumRating { get; set; }

        [Parameter(Mandatory = false)]
        public float MinimumRating { get; set; }

        [Parameter(Mandatory = false)]
        [SupportsWildcards]
        public string[] Network { get; set; }

        [Parameter(Mandatory = false)]
        [SupportsWildcards]
        public string[] Path { get; set; }

        [Parameter(Mandatory = false)]
        public int[] Tag { get; set; }

        [Parameter(Mandatory = false)]
        public SeriesType[] Type { get; set; }

        [Parameter(Mandatory = false)]
        public SeriesStatusType Status { get; set; }

        [Parameter(Mandatory = false)]
        public int[] Year { get; set; }

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

            if (this.ContainsAllParameters(x => x.MaximumRating, x => x.MinimumRating) && this.MinimumRating > this.MaximumRating)
            {
                throw new ArgumentException("The MinimumRating cannot be greater than the MaximumRating.");
            }
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

                if (this.ContainsParameter(x => x.Type))
                {
                    filtered = filtered
                        .Where(x => 
                            this.Type
                                .Contains(x.SeriesType));
                }
                if (this.ContainsParameter(x => x.Tag))
                {
                    filtered = filtered
                        .Where(x =>
                            x.Tags.Count > 0 
                            && 
                            this.Tag
                                .All(tag =>
                                    x.Tags
                                        .Contains(tag)));
                }
                else if (_noTags)
                {
                    filtered = filtered
                        .Where(x =>
                            x.Tags.Count <= 0);
                }

                if (this.ContainsParameter(x => x.Genres))
                {
                    filtered = filtered
                        .Where(x => 
                            this.Genres
                                .All(gen => 
                                    x.Genres
                                        .Contains(gen, _ig)));
                }
                if (this.ContainsParameter(x => x.IsMonitored))
                {
                    filtered = filtered.Where(x => x.IsMonitored == _isMon);
                }
                if (this.ContainsParameter(x => x.MinimumRating))
                {
                    filtered = filtered
                        .Where(x =>
                            x.Rating.CompareTo(this.MinimumRating) >= 0);
                }
                if (this.ContainsParameter(x => x.MaximumRating))
                {
                    filtered = filtered
                        .Where(x =>
                            x.Rating.CompareTo(this.MaximumRating) <= 0);
                }
                if (this.ContainsParameter(x => x.Network))
                {
                    filtered = base.FilterByStrings(filtered, x => x.Network, this.Network);
                }
                if (this.ContainsParameter(x => x.Path))
                {
                    filtered = base.FilterByStrings(filtered, x => x.Path, this.Path);
                }
                if (this.ContainsParameter(x => x.Status))
                {
                    filtered = filtered
                        .Where(x => x.Status == this.Status);
                }
                if (this.ContainsParameter(x => x.Year))
                {
                    filtered = filtered
                        .Where(x => this.Year.Contains(x.Year));
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