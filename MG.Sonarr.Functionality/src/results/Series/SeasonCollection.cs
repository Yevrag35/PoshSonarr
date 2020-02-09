using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MG.Sonarr.Results
{
    public class SeasonCollection : BaseResult, IEnumerable<Season>
    {
        #region FIELDS/CONSTANTS
        private List<Season> _list;

        #endregion

        #region INDEXERS
        public Season this[int index] => _list[index];

        #endregion

        #region PROPERTIES
        public int Count => _list.Count;
        //bool ICollection<Season>.IsReadOnly => false;

        #endregion

        #region CONSTRUCTORS
        public SeasonCollection() => _list = new List<Season>();
        internal SeasonCollection(int capacity) => _list = new List<Season>(capacity);
        [JsonConstructor]
        internal SeasonCollection(IEnumerable<Season> seasons) => _list = new List<Season>(seasons);

        #endregion

        #region PUBLIC METHODS
        //public void Add(Season season) => _list.Add(season);
        //public void Clear() => _list.Clear();
        //bool ICollection<Season>.Contains(Season item) => _list.Contains(item);
        //void ICollection<Season>.CopyTo(Season[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        //public bool Exists(Predicate<Season> match) => _list.Exists(match);
        /// <summary>
        /// Returns an array of <see cref="Season"/> instances where the specified number match the corresponding season numbers.
        /// </summary>
        /// <param name="seasonNumbers">The comma-separated list of season numbers.</param>
        /// <returns>An array of matching <see cref="Season"/> instances from the collection.</returns>
        /// <exception cref="ArgumentNullException">The parameter seasonNumbers cannot be null.</exception>
        public List<Season> GetBySeasonNumber(params int[] seasonNumbers)
        {
            if (seasonNumbers == null)
                throw new ArgumentNullException("seasonNumbers");

            return _list.FindAll(x => seasonNumbers.Contains(x.SeasonNumber));
        }
        public List<Season> GetBySeasonRange(int index, int count) => _list.GetRange(index, count);
        public IEnumerator<Season> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        public List<Season> GetMonitoredSeasons() => _list.FindAll(x => x.IsMonitored);
        public decimal GetSeasonFileSize(params int[] seasonNumbers)
        {
            if (seasonNumbers == null)
                return 0M;

            return _list
                .FindAll(s => seasonNumbers.Contains(s.SeasonNumber))
                    .Sum(s => s.Statistics.SizeOnDisk);
        }
        public decimal GetTotalFileSize() => _list.Sum(s => s.Statistics.SizeOnDisk);
        public List<Season> GetUnmonitoredSeasons() => _list.FindAll(x => !x.IsMonitored);
        //public bool Remove(Season item) => _list.Remove(item);
        public Season[] ToArray() => _list.ToArray();
        //public bool TrueForAll(Predicate<Season> match) => _list.TrueForAll(match);

        #endregion

        #region BACKEND/PRIVATE METHODS
        internal static int ToSeasonNumber(string str) => int.Parse(str);

        #endregion
    }
}