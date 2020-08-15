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
        /// The total <see cref="string"/> length of all of the contained elements in the <see cref="IUrlParameterCollection"/>.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Copies the <see cref="IUrlParameter"/> element of the specified collection to this collection.
        /// </summary>
        /// <param name="items">The collection whose <see cref="IUrlParameter"/> are copied from.</param>
        void AddRange(IEnumerable<IUrlParameter> items);

        /// <summary>
        /// Returns whether the <see cref="IUrlParameterCollection"/> contains any elements of the specific type.
        /// </summary>
        /// <typeparam name="T">The .NET type to search for that implements <see cref="IUrlParameter"/>.</typeparam>
        /// <returns>
        ///     <see langword="true"/>: if the collection contains a <see cref="IUrlParameter"/> of type <typeparamref name="T"/>;
        ///     otherwise, <see langword="false"/>.
        /// </returns>
        bool ContainsType<T>() where T : IUrlParameter;

        /// <summary>
        /// Returns all of the <see cref="IUrlParameter"/> joined together into a single query <see cref="string"/> for a <see cref="Uri"/>.
        /// </summary>
        string ToQueryString(params IUrlParameter[] oneOffs);
    }
}
