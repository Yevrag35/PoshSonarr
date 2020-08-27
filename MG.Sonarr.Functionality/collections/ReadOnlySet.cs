using MG.Sonarr.Functionality.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Collections
{
    /// <summary>
    /// An class that exposes only the read-only operations of a <see cref="SortedSet{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the set.</typeparam>
    [Serializable]
    internal class ReadOnlySet<T> : IReadOnlySet<T>, ICollection
    {
        public T this[int index]
        {
            get
            {
                int posIndex = this.GetPositiveIndex(index);
                return posIndex > -1 ? this.InnerSet.ElementAt(posIndex) : default;
            }
        }

        protected SortedSet<T> InnerSet { get; }

        public int Count => this.InnerSet.Count;
        public IComparer<T> Comparer => this.InnerSet.Comparer;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this.InnerSet;

        [JsonConstructor]
        public ReadOnlySet(IEnumerable<T> items)
        {
            IComparer<T> comparer = null;
            if (typeof(T).Equals(typeof(string)))
            {
                comparer = (IComparer<T>)SonarrFactory.NewIgnoreCaseComparer();
            }
            this.InnerSet = new SortedSet<T>(items, comparer);
        }
        public ReadOnlySet(IEnumerable<T> items, IComparer<T> comparer)
        {
            this.InnerSet = new SortedSet<T>(items, comparer);
        }

        #region ENUMERATORS
        public IEnumerator<T> GetEnumerator() => InnerSet.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        #endregion

        #region METHODS
        public bool Contains(T item) => this.InnerSet.Contains(item);
        public void CopyTo(T[] newArray, int arrayIndex) => this.InnerSet.CopyTo(newArray, arrayIndex);
        void ICollection.CopyTo(Array array, int index)
        {
            T[] tArr = new T[this.Count];
            this.CopyTo(tArr, 0);

            Array.Copy(tArr, index, array, 0, array.Length);
            Array.Clear(tArr, 0, tArr.Length);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other) => this.InnerSet.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => this.InnerSet.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => this.InnerSet.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => this.InnerSet.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => this.InnerSet.Overlaps(other);
        public bool SetEquals(IEnumerable<T> other) => this.InnerSet.SetEquals(other);

        #endregion
    }
}
