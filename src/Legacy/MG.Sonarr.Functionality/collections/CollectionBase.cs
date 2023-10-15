using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Collections
{
    /// <summary>
    /// An <see langword="abstract"/> base collection class implementing the bare minimum components for a standard read-only collection of
    /// elements.
    /// </summary>
    /// <typeparam name="T">The .NET type of each element in the collection.</typeparam>
    public abstract class CollectionBase<T> : IEnumerable<T>
    {
        /// <summary>
        /// The internal backing <see cref="List{T}"/> collection that all methods of <see cref="CollectionBase{T}"/> invoke against.
        /// </summary>
        protected List<T> InnerList { get; }

        #region PROPERTIES
        /// <summary>
        /// The number of elements contained in the <see cref="CollectionBase{T}"/>.
        /// </summary>
        public int Count => this.InnerList.Count;

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// The default constructor which initializes a new instance which is empty.
        /// </summary>
        public CollectionBase() => this.InnerList = new List<T>();
        /// <summary>
        /// Initializes a new instance of <see cref="CollectionBase{T}"/> settings the initial capacity to the specified number.
        /// </summary>
        /// <param name="capacity">The capcity to set the internal array at initially.</param>
        protected CollectionBase(int capacity) => this.InnerList = new List<T>(capacity);
        /// <summary>
        /// Initializes a new instance of <see cref="CollectionBase{T}"/> that contains elements copied from the specified
        /// collection and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="items">The collection whose items are copied into the <see cref="CollectionBase{T}"/>.</param>
        public CollectionBase(IEnumerable<T> items) => this.InnerList = new List<T>(items);

        #endregion

        #region ENUMERATORS
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CollectionBase{T}"/>.
        /// </summary>
        /// <returns>
        ///      A <see cref="IEnumerator{T}"/> for the <see cref="CollectionBase{T}"/>.
        /// </returns>
        public IEnumerator<T> GetEnumerator() => this.InnerList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.InnerList.GetEnumerator();

        #endregion
    }
}
