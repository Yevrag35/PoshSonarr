using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
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

        private AnyAllStringSet _genres;
        private List<string> _names;
        private bool _noTags;
        private HashSet<int> _ids;
        private bool _isMon;
        private IEqualityComparer<string> _ig;
        private AnyAllIntSet _anyall;

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
        public object[] Genres { get; set; }

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
        public string[] Path { get; set; }

        [Parameter(Mandatory = false)]
        public object[] Tag { get; set; }

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
            _ids = new HashSet<int>();
            if (this.ContainsParameter(x => x.Name))
                this.ProcessNamesParameter(this.Name);

            else if (this.ContainsParameter(x => x.Id))
                _ids.UnionWith(this.Id);

            if (this.ContainsParameter(x => x.Tag))
            {
                _anyall = this.AnyAllFromObjects(this.Tag);
            }

            if (this.ContainsParameter(x => x.Genres))
            {
                _genres = this.AnyAllFromGenres(this.Genres);
            }

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
                    if (_anyall.Count > 0)
                    {
                        filtered = filtered.Where(x => x.Tags.Count > 0);
                        if (_anyall.IsAll)
                        {
                            filtered = filtered.Where(x => _anyall.IsSubsetOf(x.Tags));
                        }
                        else
                        {
                            filtered = filtered.Where(x => _anyall.Overlaps(x.Tags));
                        }
                    }
                }
                else if (_noTags)
                {
                    filtered = filtered
                        .Where(x =>
                            x.Tags.Count <= 0);
                }

                if (this.ContainsParameter(x => x.Genres))
                {
                    if (_genres.IsAll)
                        filtered = filtered.Where(x => _genres.IsSubsetOf(x.Genres));

                    else
                        filtered = filtered.Where(x => _genres.Overlaps(x.Genres));
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
        
        private AnyAllIntSet AnyAllFromObjects(object[] objs)
        {
            if (this.Tag.OfType<Hashtable>().Any())
            {
                return this.Tag.OfType<Hashtable>().First();
            }
            else
            {
                return new AnyAllIntSet(this.Tag);
            }
        }
        private AnyAllStringSet AnyAllFromGenres(object[] genres)
        {
            if (this.Genres.OfType<Hashtable>().Any())
            {
                return this.Genres.OfType<Hashtable>().First();
            }
            else
            {
                return new AnyAllStringSet(this.Genres.OfType<IConvertible>().Select(x => Convert.ToString(x)));
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