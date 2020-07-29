using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results.Collections
{
    public abstract class ResultListBase<T> : ResultCollectionBase<T>, IReadOnlyList<T>
    {
        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>
        ///     The element of the specified <paramref name="index"/>.
        ///     If the index is a negative number, then the index is reversed and will count from the 
        ///     last index towards the beginning.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     index is less than 0 -or- 
        ///     index is equal to or greater than <see cref="ResultListBase{T}.Count"/>
        /// </exception>
        public T this[int index]
        {
            get
            {
                if (index >= 0)
                    return base.InnerList[index];

                else
                {
                    int goHere = base.InnerList.Count + index;
                    return goHere >= 0 ? base.InnerList[goHere] : default;
                }
            }
        }

        /// <summary>
        /// The default constructor which initializes a new instance which is empty.
        /// </summary>
        protected ResultListBase() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="ResultListBase{T}"/> that contains elements copied from the specified
        /// collection and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="items">The collection whose items are copied into the <see cref="ResultListBase{T}"/>.</param>
        protected ResultListBase(IEnumerable<T> items) : base(items) { }
    }
}
