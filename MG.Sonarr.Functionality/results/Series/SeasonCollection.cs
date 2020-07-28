using MG.Sonarr.Functionality;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MG.Sonarr.Results
{
    public class SeasonCollection : ResultListBase<Season> //BaseResult, IEnumerable<Season>
    {
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
        public Season this[int index]
        {
            get
            {
                if (index >= 0)
                    return base.InnerList[index];

                else
                {
                    int goHere = base.InnerList.Count + index;
                    return goHere >= 0 ? base.InnerList[goHere] : null;
                }
            }
        }

        #endregion

        #region PROPERTIES

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// Initializes a new instance of <see cref="SeasonCollection"/> that is empty and has the default initial capacity.
        /// </summary>
        public SeasonCollection() : base() { }
        [JsonConstructor]
        internal SeasonCollection(IEnumerable<Season> seasons) : base(seasons) { }

        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Returns a single season from the <see cref="SeasonCollection"/> that has the matching season number. 
        /// </summary>
        /// <param name="seasonNumber">The sequential season number to query for.</param>
        /// <returns>
        ///     The <see cref="Season"/> with the matching number.  If no season matches
        ///     <paramref name="seasonNumber"/>, then <see langword="null"/> is returned instead.
        /// </returns>
        public Season GetBySeasonNumber(int seasonNumber) => base.InnerList.Find(x => x.SeasonNumber == seasonNumber);

        /// <summary>
        /// Returns a list of <see cref="Season"/> instances where the specified number matches the corresponding season numbers.
        /// </summary>
        /// <param name="seasonNumbers">The comma-separated list of season numbers.</param>
        /// <returns>
        ///     An array of matching <see cref="Season"/> instances from the collection.
        ///     If no season numbers are provided, then <see langword="null"/> is returned.
        /// </returns>
        public IList<Season> GetBySeasonNumbers(params int[] seasonNumbers)
        {
            if (seasonNumbers == null || seasonNumbers.Length <= 0)
                return new List<Season>();

            return base.InnerList.FindAll(x => seasonNumbers.Contains(x.SeasonNumber));
        }

        /// <summary>
        /// Enumerates and yields all seasons where <see cref="Season.IsMonitored"/> is <see langword="true"/>.
        /// </summary>
        public IList<Season> GetMonitoredSeasons()
        {
            return base.InnerList.FindAll(x => x.IsMonitored);
        }

        /// <summary>
        /// Creates a single-dimensional array of a range of <see cref="Season"/> elements in the source collection.
        /// </summary>
        /// <param name="index">The zero-based index at which the range starts.</param>
        /// <param name="count">The number of elements in the range.</param>
        public IList<Season> GetRange(int index, int count) => base.InnerList.GetRange(index, count);

        /// <summary>
        /// Calculates the total file size (in Bytes) of each <see cref="Season"/> that 
        /// corresponds to the specified season numbers.  If no season numbers are specified,
        /// then the sum total of bytes from all seasons will be returned.
        /// </summary>
        /// <param name="seasonNumbers">The season numbers of each <see cref="Season"/> to calculate.</param>
        public long GetSeasonFileSize(params int[] seasonNumbers)
        {
            long sum = 0L;
            if (base.InnerList.Count > 0)
            {
                if (seasonNumbers == null || seasonNumbers.Length <= 0)
                {
                    sum = base.InnerList.Sum(s => s.SizeOnDisk);
                }
                else
                {
                    sum = base.InnerList
                        .FindAll(s => 
                            seasonNumbers.Contains(s.SeasonNumber))
                                .Sum(s => s.SizeOnDisk);
                }
            }
            return sum;
        }

        /// <summary>
        /// Enumerates and yields all seasons where <see cref="Season.IsMonitored"/> is <see langword="false"/>.
        /// </summary>
        public IList<Season> GetUnmonitoredSeasons() => base.InnerList.FindAll(x => !x.IsMonitored);
        /// <summary>
        /// Copies the elements of the <see cref="SeasonCollection"/> to a single-dimensional array.
        /// </summary>
        public Season[] ToArray() => base.InnerList.ToArray();

        #endregion
    }
}