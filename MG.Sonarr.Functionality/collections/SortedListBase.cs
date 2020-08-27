using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Collections
{
    public abstract class SortedStringList<TValue> : IEnumerable<TValue>
    {
        #region INDEXERS
        public TValue this[string key] => this.InnerList[key];

        #endregion

        #region PROPERTIES
        /// <summary>
        /// The number of elements contained in the <see cref="SortedStringList{TValue}"/>.
        /// </summary>
        public int Count => this.InnerList.Count;

        /// <summary>
        /// The internal backing <see cref="SortedList{TKey, TValue}"/> list that all methods of 
        /// <see cref="SortedStringList{TValue}"/> invoke against.
        /// </summary>
        protected SortedList<string, TValue> InnerList { get; }

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// The default constructor which initializes a new instance which is empty.
        /// </summary>
        public SortedStringList()
        {
            this.InnerList = new SortedList<string, TValue>(SonarrFactory.NewIgnoreCaseComparer());
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SortedStringList{TValue}"/> settings the initial capacity to the specified number.
        /// </summary>
        /// <param name="capacity">The capcity to set the internal array at initially.</param>
        public SortedStringList(int capacity) : this()
        {
            this.InnerList.Capacity = capacity;
        }

        public SortedStringList(IEnumerable<TValue> items, Func<TValue, string> getKey) : this()
        {
            if (items is ICollection icol)
                this.InnerList.Capacity = icol.Count;

            else if (items is ICollection<TValue> icol2)
                this.InnerList.Capacity = icol2.Count;

            this.AddMultiple(items, getKey);
        }

        #endregion

        #region METHODS
        protected void AddMultiple(IEnumerable<TValue> values, Func<TValue, string> getKey)
        {
            foreach (TValue val in values)
            {
                this.InnerList.Add(getKey(val), val);
            }
        }
        internal IList<TValue> GetValues() => this.InnerList.Values;

        #endregion

        #region ENUMERATORS
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="SortedStringList{TValue}"/>.
        /// </summary>
        /// <returns>
        ///      A <see cref="IEnumerator{TValue}"/> for the <see cref="SortedStringList{TValue}"/>.
        /// </returns>
        public IEnumerator<TValue> GetEnumerator() => this.InnerList.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.InnerList.GetEnumerator();

        #endregion
    }
}
