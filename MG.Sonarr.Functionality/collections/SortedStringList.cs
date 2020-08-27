using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Collections
{
    public abstract class SortedStringList<TValue> : SortedListBase<string, TValue>
    {
        #region INDEXERS
        public TValue this[string key] => this.InnerList[key];

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// The default constructor which initializes a new instance which is empty.
        /// </summary>
        public SortedStringList() 
            : base(SonarrFactory.NewIgnoreCaseComparer())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SortedStringList{TValue}"/> settings the initial capacity to the specified number.
        /// </summary>
        /// <param name="capacity">The capcity to set the internal array at initially.</param>
        public SortedStringList(int capacity) 
            : base(capacity, SonarrFactory.NewIgnoreCaseComparer())
        {
        }

        public SortedStringList(IEnumerable<TValue> items, Func<TValue, string> getKey)
            : base(items, SonarrFactory.NewIgnoreCaseComparer(), getKey)
        {
        }

        #endregion
    }
}
