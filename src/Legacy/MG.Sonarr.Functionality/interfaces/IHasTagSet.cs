using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// An interfaces exposing an object's unique set of tag ID's.
    /// </summary>
    public interface IHasTagSet
    {
        /// <summary>
        /// The list of unique tag ID's associated with a given object.
        /// </summary>
        HashSet<int> Tags { get; }
    }
}
