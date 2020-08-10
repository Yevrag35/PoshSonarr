using MG.Sonarr.Results;
using MG.Sonarr.Results.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Collections
{
    /// <summary>
    /// Provides a collection class for <see cref="Tag"/> instances while keeping most
    /// of the <see cref="ICollection"/> and <see cref="IList"/> methods hidden.
	/// </summary>
    internal sealed class TagCollection : HashSet<Tag>, ITagCollection
    {

        #region CONSTRUCTORS
        /// <summary>
        /// Initializes a new instance of the <see cref="TagCollection{T}"/> class that is empty
        /// and has the default initial capacity.
        /// </summary>
        internal TagCollection() : base()
        {
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
        }

        #endregion

        #region BACKEND/PRIVATE METHODS
        //internal void Clear() => this.InnerList.Clear();
        public bool Contains(int tagId) => this.Any(x => x.Id.Equals(tagId));
        //internal int FindIndex(Predicate<Tag> match) => this.InnerList.FindIndex(match);
        //internal bool Remove(Tag tag) => this.InnerList.Remove(tag);
        //internal void RemoveAll(Predicate<Tag> match) => this.InnerList.RemoveAll(match);
        internal void SetTag(int tagId, string newLabel)
        {
            Tag tag = this.FirstOrDefault<Tag>(x => x.Id.Equals(tagId));
            if (tag != null)
                tag.Label = newLabel;
        }

        #endregion
    }
}