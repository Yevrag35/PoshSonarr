using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// An interface that exposes properties that support tag-updating operations.
    /// </summary>
    public interface ISupportsTagUpdate : IGetEndpoint, IJsonResult
    {
        /// <summary>
        /// The ID of the object to update.
        /// </summary>
        object Id { get; }

        /// <summary>
        /// The list of unique tag ID's associated with a given object.
        /// </summary>
        HashSet<int> Tags { get; set; }
    }
}
