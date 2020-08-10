using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality.Collections
{
    /// <summary>
    /// An interface exposing <see cref="IEnumerable{T}"/> methods, indexing, and a count of <see cref="Tag"/> instances.
    /// </summary>
    public interface ITagCollection : IReadOnlyCollection<Tag>
    {

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

        bool IsSubsetOf(IEnumerable<Tag> other);
        bool IsSupersetOf(IEnumerable<Tag> other);
        bool Overlaps(IEnumerable<Tag> other);
        bool SetEquals(IEnumerable<Tag> other);

    }
}
