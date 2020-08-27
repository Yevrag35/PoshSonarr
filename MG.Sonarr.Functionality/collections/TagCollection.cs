using MG.Sonarr.Functionality.Extensions;
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
    internal sealed class TagCollection : ITagCollection
    {
        private SortedList<int, Tag> _list;

        public int Count => _list.Count;

        public Tag this[int index]
        {
            get
            {
                int posIndex = this.GetPositiveIndex(index);
                return posIndex > -1 ? _list.Values[posIndex] : null;
            }
        }

        #region CONSTRUCTORS
        /// <summary>
        /// Initializes a new instance of the <see cref="TagCollection{T}"/> class that is empty
        /// and has the default initial capacity.
        /// </summary>
        internal TagCollection()
        {
            _list = new SortedList<int, Tag>();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TagCollection{T}"/> class that
        /// contains elements copied from the specified <see cref="IEnumerable{T}"/> and has
        /// sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="tags">The collection whose elements are copied to the new list.</param>
        /// <exception cref="ArgumentNullException"/>
        internal TagCollection(IEnumerable<Tag> tags)
        {
            int size = 0;
            if (tags is ICollection<Tag> tagCol)
                size = tagCol.Count;

            else if (tags is IReadOnlyCollection<Tag> readOnlyCol)
                size = readOnlyCol.Count;

            _list = new SortedList<int, Tag>(size);
            foreach (Tag tag in tags)
            {
                _list.Add(tag.Id, tag);
            }
        }

        #endregion

        #region BACKEND/PRIVATE METHODS
        public void Add(Tag tag)
        {
            if (tag == null)
                return;

            else if (!_list.ContainsKey(tag.Id))
                _list.Add(tag.Id, tag);
        }
        public bool Contains(int tagId) => _list.ContainsKey(tagId);
        public bool Contains(Tag tag) => _list.Values.Contains(tag);
        public IEnumerator<Tag> GetEnumerator() => _list.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        public bool Overlaps(IEnumerable<Tag> other)
        {
            return other.Any(x => _list.Keys.Contains(x.Id));
        }
        public bool Remove(int id) => _list.Remove(id);
        public bool Remove(Tag tag) => _list.Remove(tag.Id);
        public void RemoveMissing(IEnumerable<Tag> other)
        {
            for (int i = _list.Values.Count - 1; i >= 0; i--)
            {
                Tag t = _list.Values[i];
                if (!other.Contains(t))
                {
                    _list.Remove(t.Id);
                }
            }
        }
        internal void SetTag(int tagId, string newLabel)
        {
            if (_list.TryGetValue(tagId, out Tag tag))
            {
                tag.Label = newLabel;
            }
        }
        public void TrimExcess() => _list.TrimExcess();
        public void UnionWith(IEnumerable<Tag> other)
        {
            foreach (Tag tag in other)
            {
                if (!_list.ContainsKey(tag.Id))
                {
                    _list.Add(tag.Id, tag);
                }
            }
        }

        #endregion
    }
}