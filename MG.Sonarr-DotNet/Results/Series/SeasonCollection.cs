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
    public class SeasonCollection : BaseResult, ICollection<Season>
    {
        #region FIELDS/CONSTANTS
        private static readonly char HYPHEN = char.Parse("-");
        private static readonly char COMMA = char.Parse(",");
        private List<Season> _list;

        #endregion

        #region INDEXERS
        public Season this[int index] => _list[index];

        #endregion

        #region PROPERTIES
        public int Count => _list.Count;
        bool ICollection<Season>.IsReadOnly => false;

        #endregion

        #region CONSTRUCTORS
        public SeasonCollection() => _list = new List<Season>();
        public SeasonCollection(int capacity) => _list = new List<Season>(capacity);
        internal SeasonCollection(IEnumerable<Season> seasons) => _list = new List<Season>(seasons);

        #endregion

        #region PUBLIC METHODS
        public void Add(Season season) => _list.Add(season);
        public void Clear() => _list.Clear();
        bool ICollection<Season>.Contains(Season item) => _list.Contains(item);
        void ICollection<Season>.CopyTo(Season[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public bool Exists(Predicate<Season> match) => _list.Exists(match);
        public Season[] GetBySeasonNumber(params int[] seasonNumbers)
        {
            if (seasonNumbers == null)
                throw new ArgumentNullException("seasonNumbers");

            return _list.FindAll(x => seasonNumbers.Contains(x.SeasonNumber)).ToArray();
        }
        public Season[] GetBySeasonRange(int rangeLow, int rangeHigh) => 
            _list.FindAll(x => x.SeasonNumber <= rangeHigh && x.SeasonNumber >= rangeLow).ToArray();
        public IEnumerator<Season> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        public SeasonCollection GetMonitoredSeasons() => new SeasonCollection(_list.FindAll(x => x.Monitored));
        public SeasonCollection GetUnmonitoredSeasons() => new SeasonCollection(_list.FindAll(x => !x.Monitored));
        public bool Remove(Season item) => _list.Remove(item);
        public Season[] ToArray() => _list.ToArray();
        public bool TrueForAll(Predicate<Season> match) => _list.TrueForAll(match);

        #endregion

        #region BACKEND/PRIVATE METHODS
        internal static int ToSeasonNumber(string str) => int.Parse(str);

        private class SeasonRange
        {
            public int High { get; }
            public int Low { get; }

            private SeasonRange(int[] range)
            {
                this.High = range.Max();
                this.Low = range.Min();
            }

            public static implicit operator SeasonRange(int[] range)
            {
                if (range.Length < 2)
                    return null;

                return new SeasonRange(range);
            }
        }

        #endregion
    }
}