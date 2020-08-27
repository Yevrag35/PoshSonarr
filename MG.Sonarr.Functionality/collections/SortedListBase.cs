using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Collections
{
    public abstract class SortedListBase<TKey, TValue> : IEnumerable<TValue>
    {
        #region PROPERTIES
        /// <summary>
        /// The number of elements contained in the <see cref="SortedStringList{TValue}"/>.
        /// </summary>
        public int Count => this.InnerList.Count;

        /// <summary>
        /// The internal backing <see cref="SortedList{TKey, TValue}"/> list that all methods of 
        /// <see cref="SortedStringList{TValue}"/> invoke against.
        /// </summary>
        protected SortedList<TKey, TValue> InnerList { get; }

        #endregion

        #region CONSTRUCTORS
        public SortedListBase()
        {
            this.InnerList = new SortedList<TKey, TValue>();
        }
        public SortedListBase(int capacity)
            : this()
        {
            this.InnerList.Capacity = capacity;
        }
        public SortedListBase(IComparer<TKey> comparer)
        {
            this.InnerList = new SortedList<TKey, TValue>(comparer);
        }
        public SortedListBase(int capacity, IComparer<TKey> comparer)
            : this(comparer)
        {
            this.InnerList.Capacity = capacity;
        }
        public SortedListBase(IEnumerable<TValue> items, Func<TValue, TKey> getKey)
            : this()
        {
            if (items is ICollection icol)
                this.InnerList.Capacity = icol.Count;

            else if (items is ICollection<TValue> icol2)
                this.InnerList.Capacity = icol2.Count;

            this.AddMultiple(items, getKey);
        }
        public SortedListBase(IEnumerable<TValue> items, IComparer<TKey> comparer, Func<TValue, TKey> getKey)
            : this(comparer)
        {
            if (items is ICollection icol)
                this.InnerList.Capacity = icol.Count;

            else if (items is ICollection<TValue> icol2)
                this.InnerList.Capacity = icol2.Count;

            this.AddMultiple(items, getKey);
        }

        #endregion

        #region METHODS
        protected void AddMultiple(IEnumerable<TValue> values, Func<TValue, TKey> getKey)
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
