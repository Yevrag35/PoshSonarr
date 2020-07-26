using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Collections
{
    /// <summary>
    /// An interface representing a collection of <see cref="IUrlParameter"/> which can serialized to a single <see cref="string"/>
    /// in the form of an <see cref="Uri"/> query.
    /// </summary>
    public interface IUrlParameterCollection : IList<IUrlParameter>
    {
        /// <summary>
        /// Copies the <see cref="IUrlParameter"/> element of the specified collection to this collection.
        /// </summary>
        /// <param name="items">The collection whose <see cref="IUrlParameter"/> are copied from.</param>
        void AddRange(IEnumerable<IUrlParameter> items);

        /// <summary>
        /// Returns all of the <see cref="IUrlParameter"/> joined together into a single query <see cref="string"/> for a <see cref="Uri"/>.
        /// </summary>
        string ToQueryString();
    }
}
