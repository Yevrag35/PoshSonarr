using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Collections
{
    /// <summary>
    /// An interface exposing <see cref="IEnumerable{T}"/> methods, indexing, and a count of <see cref="Tag"/> instances.
    /// </summary>
    public interface ITagCollection : IEnumerable<Tag>
    {
        Tag this[int index] { get; }

        /// <summary>
        /// The number of <see cref="Tag"/> elements the current collection holds.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Returns whether or not the current collection contains a <see cref="Tag"/> whose
        /// ID matches the specified integer.
        /// </summary>
        /// <param name="tagId">The ID of the tag to search for.</param>
        bool Contains(int tagId);

        /// <summary>
        /// Returns whether or not the current contains the specified <see cref="Tag"/>.
        /// </summary>
        /// <param name="tag">The tag to search for.</param>
        bool Contains(Tag tag);

        /// <summary>
        /// Sorts the elements of this collection using the default sort comparer of <see cref="Tag"/>.
        /// </summary>
        void Sort();

        /// <summary>
        /// Sorts the elements of this collection using the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer used to sort the collection by.</param>
        void Sort(IComparer<Tag> comparer);
    }
}
