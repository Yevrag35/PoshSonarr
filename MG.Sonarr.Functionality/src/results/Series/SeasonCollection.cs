using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MG.Sonarr.Results
{
    public class SeasonCollection : BaseResult, IEnumerable<Season>
    {
        #region FIELDS/CONSTANTS
        private List<Season> _list;

        #endregion

        #region INDEXERS
        /// <summary>
        /// Gets the <see cref="Season"/> element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The <see cref="Season"/> element of the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     index is less than 0 -or- 
        ///     index is equal to or greater than <see cref="SeasonCollection.Count"/>
        /// </exception>
        public Season this[int index] => _list[index];

        #endregion

        #region PROPERTIES
        /// <summary>
        /// Gets the number of <see cref="Season"/> elements contained within the collection.
        /// </summary>
        public int Count => _list.Count;

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// Initializes a new instance of <see cref="SeasonCollection"/> that is empty and has the default initial capacity.
        /// </summary>
        public SeasonCollection() => _list = new List<Season>();
        internal SeasonCollection(int capacity) => _list = new List<Season>(capacity);
        [JsonConstructor]
        internal SeasonCollection(IEnumerable<Season> seasons) => _list = new List<Season>(seasons);

        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Returns an array of <see cref="Season"/> instances where the specified number match the corresponding season numbers.
        /// </summary>
        /// <param name="seasonNumbers">The comma-separated list of season numbers.</param>
        /// <returns>An array of matching <see cref="Season"/> instances from the collection.</returns>
        /// <exception cref="ArgumentNullException">The parameter seasonNumbers cannot be null.</exception>
        public Season[] GetBySeasonNumber(params int[] seasonNumbers)
        {
            if (seasonNumbers == null)
                throw new ArgumentNullException("seasonNumbers");

            return _list.Where(x => seasonNumbers.Contains(x.SeasonNumber)).ToArray();
        }
        /// <summary>
        /// Creates a single-dimensional array of a range of <see cref="Season"/> elements in the source collection.
        /// </summary>
        /// <param name="index">The zero-based index at which the range starts.</param>
        /// <param name="count">The number of elements in the range.</param>
        public Season[] GetBySeasonRange(int index, int count) => _list.Skip(index).Take(count).ToArray();
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="SeasonCollection"/>.
        /// </summary>
        public IEnumerator<Season> GetEnumerator() => _list.GetEnumerator();
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="SeasonCollection"/>.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        /// <summary>
        /// Enumerates and yields all seasons where <see cref="Season.IsMonitored"/> is <see langword="true"/>.
        /// </summary>
        public IEnumerable<Season> GetMonitoredSeasons()
        {
            foreach (Season s in _list)
            {
                if (s.IsMonitored)
                    yield return s;
            }
        }
        /// <summary>
        /// Calculates the total file size (in Bytes) of each <see cref="Season"/> that corresponds to the specified season numbers.
        /// </summary>
        /// <param name="seasonNumbers">The season numbers of each <see cref="Season"/> to calculate.</param>
        public long GetSeasonFileSize(params int[] seasonNumbers)
        {
            long sum;
            if (seasonNumbers == null || seasonNumbers.Length <= 0)
            {
                sum = _list.Sum(s => s.SizeOnDisk);
            }
            else
            {
                sum = _list
                    .FindAll(s => seasonNumbers.Contains(s.SeasonNumber))
                        .Sum(s => s.SizeOnDisk);
            }
            return sum;
        }
        ///// <summary>
        ///// Calculates the total file size (in Bytes) of all <see cref="Season"/> elements contained within the collection.
        ///// </summary>
        //public decimal GetTotalFileSize() => _list.Sum(s => s.SizeOnDisk);
        /// <summary>
        /// Enumerates and yields all seasons where <see cref="Season.IsMonitored"/> is <see langword="false"/>.
        /// </summary>
        public IEnumerable<Season> GetUnmonitoredSeasons()
        {
            foreach (Season s in _list)
            {
                if (!s.IsMonitored)
                    yield return s;
            }
        }
        /// <summary>
        /// Copies the elements of the <see cref="SeasonCollection"/> to a single-dimensional array.
        /// </summary>
        public Season[] ToArray() => _list.ToArray();

        #endregion
    }
}