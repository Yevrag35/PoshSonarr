using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Series", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "BySeriesName")]
    [OutputType(typeof(SeriesResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetSeries : SeriesCmdlet
    {
        #region FIELDS/CONSTANTS

        internal AnyAllIntSet _anyall;
        private Func<SeriesResult, string> _func;
        internal AnyAllStringSet _genres;
        internal HashSet<int> _ids;
        internal bool _isDebugging;
        internal bool _isMon;
        //private List<string> _names;
        private HashSet<string> _names;
        private bool _noTags;
        private const string REGEX_PATTERN = "^.+\\s+(?:\\-n|\\\"|\\')";

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
        public SeriesStatusType Status { get; set; }

        [Parameter(Mandatory = false)]
        public object[] Tag { get; set; }

        [Parameter(Mandatory = false)]
        public long[] TVDBId { get; set; }

        [Parameter(Mandatory = false)]
        public SeriesType[] Type { get; set; }

        [Parameter(Mandatory = false)]
        public int[] Year { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            if (this.ContainsAllParameters("Debug"))
            {
                _isDebugging = ((SwitchParameter)this.MyInvocation.BoundParameters["Debug"]).ToBool();
                if (_isDebugging)
                    _func = x => x.CleanTitle;
            }

            //_names = new List<string>();
            _names = new HashSet<string>(ClassFactory.NewIgnoreCase());
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
                base.SendToPipeline(base.GetSeriesById(_ids, _isDebugging));
            }
            else
            {
                IEnumerable<SeriesResult> filtered = base.GetAllSeries(_isDebugging);

                filtered = this
                    .FilterByName(filtered)
                        .ThenFilterBy(this,
                            p => p.Tag,
                            c => _anyall.Count > 0,
                            w => w.Tags.Count > 0)
                        .ThenFilterBy(this,
                            p => p.Tag,
                            c => _anyall.IsAll,
                            w => _anyall.IsSubsetOf(w.Tags))
                        .ThenFilterBy(this,
                            p => p.Tag,
                            c => !_anyall.IsAll,
                            w => _anyall.Overlaps(w.Tags))
                        .ThenFilterBy(this,
                            p => p.HasNoTags,
                            c => _noTags,
                            w => w.Tags.Count <= 0)
                        .ThenFilterBy(this,
                            p => p.Type,
                            null,
                            w => this.Type.Contains(w.SeriesType))
                        .ThenFilterBy(this,
                            p => p.Genres,
                            c => _genres.IsAll,
                            w => _genres.IsSubsetOf(w.Genres))
                        .ThenFilterBy(this,
                            p => p.Genres,
                            c => !_genres.IsAll,
                            w => _genres.Overlaps(w.Genres))
                        .ThenFilterBy(this,
                            p => p.IsMonitored,
                            null,
                            w => w.IsMonitored == _isMon)
                        .ThenFilterBy(this,
                            p => p.MinimumRating,
                            null,
                            w => w.Rating.CompareTo(this.MinimumRating) >= 0)
                        .ThenFilterBy(this,
                            p => p.MaximumRating,
                            null,
                            w => w.Rating.CompareTo(this.MaximumRating) <= 0)
                        .ThenFilterBy(this,
                            p => p.Status,
                            null,
                            w => w.Status == this.Status)
                        .ThenFilterBy(this,
                            p => p.Year,
                            null,
                            w => this.Year.Contains(w.Year))
                        .ThenFilterByValues(this,
                            p => p.TVDBId,
                            m => m.TVDBId,
                            this.TVDBId)
                        .ThenFilterByStrings(this, 
                            p => p.Path, 
                            m => m.Path,
                            this.Path);

                base.SendToPipeline(filtered);
            }
        }

        #endregion

        #region BACKEND METHODS

        #region FILTERING METHODS

        private IEnumerable<SeriesResult> FilterByName(IEnumerable<SeriesResult> filterThis)
        {
            filterThis = base.FilterByStrings(filterThis, x => x.Name, _names.Count > 0 ? _names : null);
            return filterThis;
        }
        

        #endregion

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
        private string GetInvocation()
        {
            return this.MyInvocation.Line.Substring(this.MyInvocation.OffsetInLine - 1);
        }
        private bool Matches()
        {
            return Regex.IsMatch(this.GetInvocation(), REGEX_PATTERN, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        private void ProcessNamesParameter(object[] objNames)
        {
            if (this.Matches())
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