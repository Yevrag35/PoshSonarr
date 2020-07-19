using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// Provides a collection class for <see cref="Tag"/> instances while keeping most
    /// of the <see cref="ICollection"/> and <see cref="IList"/> methods hidden.
	/// </summary>
    public sealed class TagCollection : ResultCollectionBase<Tag>, ITagCollection //IEnumerable<Tag>
    {
        #region INDEXERS
        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-bsaed index of the element to get.</param>
        public Tag this[int index] => this.InnerList[index];

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// Initializes a new instance of the <see cref="TagCollection{T}"/> class that is empty
        /// and has the default initial capacity.
        /// </summary>
        internal TagCollection() : base()
        {
            //this.InnerList = new List<Tag>();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TagCollection{T}"/> class that is empty
        /// and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new collection can initially store.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        internal TagCollection(int capacity) : base(capacity)
        {
            //this.InnerList = new List<Tag>(capacity);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TagCollection{T}"/> class that
        /// contains elements copied from the specified <see cref="IEnumerable{T}"/> and has
        /// sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="tags">The collection whose elements are copied to the new list.</param>
        /// <exception cref="ArgumentNullException"/>
        internal TagCollection(IEnumerable<Tag> tags) : base(tags)
        {
            //this.InnerList = new List<Tag>(tags);
        }

        #endregion

        #region BASE METHODS
        public void Sort() => this.InnerList.Sort();
        public void Sort(IComparer<Tag> comparer) => this.InnerList.Sort(comparer);

        #endregion

        #region ENUMERATOR
        //public IEnumerator<Tag> GetEnumerator() => this.InnerList.GetEnumerator();
        //IEnumerator IEnumerable.GetEnumerator() => this.InnerList.GetEnumerator();

        #endregion

        #region BACKEND/PRIVATE METHODS
        internal int Add(Tag tag)
        {
            this.InnerList.Add(tag);
            return tag.Id;
        }
        internal void Clear() => this.InnerList.Clear();
        public bool Contains(int tagId) => this.InnerList.Exists(x => x.Id == tagId);
        //internal bool Contains(Tag tag) => this.InnerList.Contains(tag);
        //internal bool Contains(Predicate<Tag> match) => this.InnerList.Exists(match);
        //internal Tag Find(Predicate<Tag> match) => this.InnerList.Find(match);
        //internal List<Tag> FindAll(Predicate<Tag> match) => this.InnerList.FindAll(match);
        internal int FindIndex(Predicate<Tag> match) => this.InnerList.FindIndex(match);
        //internal int IndexOf(Tag tag) => this.InnerList.IndexOf(tag);
        internal bool Remove(Tag tag) => this.InnerList.Remove(tag);
        internal void RemoveAll(Predicate<Tag> match) => this.InnerList.RemoveAll(match);
        internal void SetTag(int tagId, string newLabel)
        {
            if (this.Contains(tagId))
            {
                int index = this.InnerList.FindIndex(x => x.Id == tagId);
                this.InnerList[index].Label = newLabel;
            }
        }
        //internal void TrueForAll(Predicate<Tag> match) => this.InnerList.TrueForAll(match);

        #endregion
    }
}