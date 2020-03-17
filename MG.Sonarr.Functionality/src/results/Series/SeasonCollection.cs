﻿using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MG.Sonarr.Results
{
    public class SeasonCollection : ResultCollectionBase<Season> //BaseResult, IEnumerable<Season>
    {
        #region FIELDS/CONSTANTS
        //private List<Season> base.InnerList;

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
        //public Season this[int index] => base.InnerList[index];

        #endregion

        #region PROPERTIES
        /// <summary>
        /// Gets the number of <see cref="Season"/> elements contained within the collection.
        /// </summary>
        //public int Count => base.InnerList.Count;

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// Initializes a new instance of <see cref="SeasonCollection"/> that is empty and has the default initial capacity.
        /// </summary>
        public SeasonCollection() : base() { }//=> base.InnerList = new List<Season>();
        internal SeasonCollection(int capacity) : base(capacity) { }//=> base.InnerList = new List<Season>(capacity);
        [JsonConstructor]
        internal SeasonCollection(IEnumerable<Season> seasons) : base(seasons) { }//=> base.InnerList = new List<Season>(seasons);

        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Returns a list of <see cref="Season"/> instances where the specified number matches the corresponding season numbers.
        /// </summary>
        /// <param name="seasonNumbers">The comma-separated list of season numbers.</param>
        /// <returns>
        ///     An array of matching <see cref="Season"/> instances from the collection.
        ///     If no season numbers are provided, then <see langword="null"/> is returned.
        /// </returns>
        public List<Season> GetBySeasonNumber(params int[] seasonNumbers)
        {
            if (seasonNumbers == null)
                return null;

            return base.InnerList.FindAll(x => seasonNumbers.Contains(x.SeasonNumber));
        }
        /// <summary>
        /// Creates a single-dimensional array of a range of <see cref="Season"/> elements in the source collection.
        /// </summary>
        /// <param name="index">The zero-based index at which the range starts.</param>
        /// <param name="count">The number of elements in the range.</param>
        public Season[] GetBySeasonRange(int index, int count) => base.InnerList.Skip(index).Take(count).ToArray();

        /// <summary>
        /// Enumerates and yields all seasons where <see cref="Season.IsMonitored"/> is <see langword="true"/>.
        /// </summary>
        public IEnumerable<Season> GetMonitoredSeasons() => base.InnerList.FindAll(x => x.IsMonitored);

        /// <summary>
        /// Calculates the total file size (in Bytes) of each <see cref="Season"/> that corresponds to the specified season numbers.
        /// </summary>
        /// <param name="seasonNumbers">The season numbers of each <see cref="Season"/> to calculate.</param>
        public long GetSeasonFileSize(params int[] seasonNumbers)
        {
            long sum;
            if (seasonNumbers == null || seasonNumbers.Length <= 0)
            {
                sum = base.InnerList.Sum(s => s.SizeOnDisk);
            }
            else
            {
                sum = base.InnerList
                    .FindAll(s => seasonNumbers.Contains(s.SeasonNumber))
                        .Sum(s => s.SizeOnDisk);
            }
            return sum;
        }

        /// <summary>
        /// Enumerates and yields all seasons where <see cref="Season.IsMonitored"/> is <see langword="false"/>.
        /// </summary>
        public IEnumerable<Season> GetUnmonitoredSeasons() => base.InnerList.FindAll(x => !x.IsMonitored);
        /// <summary>
        /// Copies the elements of the <see cref="SeasonCollection"/> to a single-dimensional array.
        /// </summary>
        public Season[] ToArray() => base.InnerList.ToArray();

        #endregion
    }
}